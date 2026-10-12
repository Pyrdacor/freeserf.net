/*
 * SaveGameDocumentsProvider.cs - Shows the save games in the Android files app
 *
 * Copyright (C) 2026  Robert Schneckenhaus <robert.schneckenhaus@web.de>
 *
 * This file is part of freeserf.net. freeserf.net is based on freeserf.
 *
 * freeserf.net is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * freeserf.net is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with freeserf.net. If not, see <http://www.gnu.org/licenses/>.
 */

using Android.App;
using Android.Content;
using Android.Database;
using Android.OS;
using Android.Provider;
using System;
using System.IO;
using System.Linq;

namespace Freeserf.Android
{
    /// <summary>
    /// Makes the save game folder accessible in the Android files app and
    /// other file managers which use the storage access framework. The save
    /// games of older versions which were stored in the app's private storage
    /// are shown in a sub folder.
    /// </summary>
    [ContentProvider(new[] { Authority }, Exported = true, GrantUriPermissions = true,
        Permission = "android.permission.MANAGE_DOCUMENTS")]
    [IntentFilter(new[] { "android.content.action.DOCUMENTS_PROVIDER" })]
    public class SaveGameDocumentsProvider : DocumentsProvider
    {
        public const string Authority = "net.freeserf.android.documents";

        const string RootId = "freeserf";
        const string SavesFolderId = "saves";
        const string OldSavesFolderId = "old";
        const string OldSavesFolderName = "Old saves";
        const string MimeTypeDirectory = "vnd.android.document/directory";
        const string MimeTypeFile = "application/octet-stream";

        // Column names and flags of DocumentsContract.Root and DocumentsContract.Document.
        const string ColumnRootId = "root_id";
        const string ColumnRootFlags = "flags";
        const string ColumnRootIcon = "icon";
        const string ColumnRootTitle = "title";
        const string ColumnRootDocumentId = "document_id";
        const string ColumnDocumentId = "document_id";
        const string ColumnDisplayName = "_display_name";
        const string ColumnMimeType = "mime_type";
        const string ColumnFlags = "flags";
        const string ColumnSize = "_size";
        const string ColumnLastModified = "last_modified";
        const int RootFlagSupportsCreate = 0x1;
        const int RootFlagLocalOnly = 0x2;
        const int RootFlagSupportsIsChild = 0x10;
        const int DocumentFlagSupportsWrite = 0x2;
        const int DocumentFlagSupportsDelete = 0x4;
        const int DocumentFlagDirSupportsCreate = 0x8;
        const int DocumentFlagSupportsRename = 0x40;

        static readonly string[] DefaultRootProjection =
        [
            ColumnRootId, ColumnRootFlags, ColumnRootIcon, ColumnRootTitle, ColumnRootDocumentId
        ];

        static readonly string[] DefaultDocumentProjection =
        [
            ColumnDocumentId, ColumnDisplayName, ColumnMimeType, ColumnFlags, ColumnSize, ColumnLastModified
        ];

        string savesFolder = null;
        string oldSavesFolder = null;

        public override bool OnCreate()
        {
            // Same folders as used by the game (see MainActivity and FileSystem.Paths).
            var externalFilesDir = Context.GetExternalFilesDir(null);
            string personalFolder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);

            oldSavesFolder = Path.Combine(personalFolder, "freeserf", "saves");
            savesFolder = externalFilesDir == null ? oldSavesFolder : Path.Combine(externalFilesDir.AbsolutePath, "saves");

            if (savesFolder == oldSavesFolder)
                oldSavesFolder = null;

            return true;
        }

        public override ICursor QueryRoots(string[] projection)
        {
            var cursor = new MatrixCursor(projection ?? DefaultRootProjection);
            var row = cursor.NewRow();

            row.Add(ColumnRootId, RootId);
            row.Add(ColumnRootFlags, RootFlagSupportsCreate | RootFlagLocalOnly | RootFlagSupportsIsChild);
            row.Add(ColumnRootIcon, Resource.Mipmap.ic_launcher);
            row.Add(ColumnRootTitle, Context.GetString(Resource.String.app_name));
            row.Add(ColumnRootDocumentId, SavesFolderId);

            return cursor;
        }

        public override ICursor QueryDocument(string documentId, string[] projection)
        {
            var cursor = new MatrixCursor(projection ?? DefaultDocumentProjection);

            AddDocument(cursor, documentId);

            return cursor;
        }

        public override ICursor QueryChildDocuments(string parentDocumentId, string[] projection, string sortOrder)
        {
            var cursor = new MatrixCursor(projection ?? DefaultDocumentProjection);
            string folder = GetPath(parentDocumentId);

            if (Directory.Exists(folder))
            {
                foreach (var file in Directory.GetFiles(folder).OrderBy(file => file))
                    AddDocument(cursor, parentDocumentId + "/" + Path.GetFileName(file));
            }

            if (parentDocumentId == SavesFolderId && HasOldSaves())
                AddDocument(cursor, OldSavesFolderId);

            // Let the files app update the list when files change.
            cursor.SetNotificationUri(Context.ContentResolver, DocumentsContract.BuildChildDocumentsUri(Authority, parentDocumentId));

            return cursor;
        }

        public override ParcelFileDescriptor OpenDocument(string documentId, string mode, CancellationSignal signal)
        {
            string path = GetPath(documentId);

            if (!IsFile(documentId))
                throw new Java.IO.FileNotFoundException(documentId);

            var fileMode = ParcelFileDescriptor.ParseMode(mode);

            // Old saves are read-only. New ones are stored in the saves folder.
            if (documentId.StartsWith(OldSavesFolderId + "/") && mode != "r")
                throw new Java.Lang.UnsupportedOperationException("Old saves are read-only.");

            return ParcelFileDescriptor.Open(new Java.IO.File(path), fileMode);
        }

        public override string CreateDocument(string parentDocumentId, string mimeType, string displayName)
        {
            if (parentDocumentId != SavesFolderId || mimeType == MimeTypeDirectory)
                throw new Java.Lang.UnsupportedOperationException("Only save game files can be created.");

            Directory.CreateDirectory(savesFolder);

            string name = GetValidFileName(displayName);
            string baseName = Path.GetFileNameWithoutExtension(name);
            string extension = Path.GetExtension(name);

            // Never overwrite existing files.
            for (int i = 1; File.Exists(Path.Combine(savesFolder, name)); ++i)
                name = $"{baseName} ({i}){extension}";

            File.Create(Path.Combine(savesFolder, name)).Dispose();
            NotifyChanged(SavesFolderId);

            return SavesFolderId + "/" + name;
        }

        public override void DeleteDocument(string documentId)
        {
            if (!IsFile(documentId))
                throw new Java.Lang.UnsupportedOperationException("Only files can be deleted.");

            File.Delete(GetPath(documentId));
            NotifyChanged(GetParentId(documentId));
        }

        public override string RenameDocument(string documentId, string displayName)
        {
            if (!IsFile(documentId) || !documentId.StartsWith(SavesFolderId + "/"))
                throw new Java.Lang.UnsupportedOperationException("Only save games can be renamed.");

            string name = GetValidFileName(displayName);
            string newPath = Path.Combine(savesFolder, name);

            if (File.Exists(newPath))
                throw new Java.Lang.IllegalStateException($"A file named {name} already exists.");

            File.Move(GetPath(documentId), newPath);
            NotifyChanged(SavesFolderId);

            return SavesFolderId + "/" + name;
        }

        public override bool IsChildDocument(string parentDocumentId, string documentId)
        {
            return documentId.StartsWith(parentDocumentId + "/") ||
                (parentDocumentId == SavesFolderId && documentId.StartsWith(OldSavesFolderId));
        }

        bool HasOldSaves()
        {
            return oldSavesFolder != null && Directory.Exists(oldSavesFolder) && Directory.GetFiles(oldSavesFolder).Length != 0;
        }

        void AddDocument(MatrixCursor cursor, string documentId)
        {
            var row = cursor.NewRow();

            row.Add(ColumnDocumentId, documentId);

            if (documentId == SavesFolderId || documentId == OldSavesFolderId)
            {
                bool saves = documentId == SavesFolderId;

                row.Add(ColumnDisplayName, saves ? Context.GetString(Resource.String.app_name) : OldSavesFolderName);
                row.Add(ColumnMimeType, MimeTypeDirectory);
                row.Add(ColumnFlags, saves ? DocumentFlagDirSupportsCreate : 0);
                row.Add(ColumnSize, null);
                row.Add(ColumnLastModified, null);
            }
            else
            {
                var file = new FileInfo(GetPath(documentId));

                if (!file.Exists)
                    throw new Java.IO.FileNotFoundException(documentId);

                int flags = DocumentFlagSupportsDelete;

                if (documentId.StartsWith(SavesFolderId + "/"))
                    flags |= DocumentFlagSupportsWrite | DocumentFlagSupportsRename;

                row.Add(ColumnDisplayName, file.Name);
                row.Add(ColumnMimeType, MimeTypeFile);
                row.Add(ColumnFlags, flags);
                row.Add(ColumnSize, file.Length);
                row.Add(ColumnLastModified, new DateTimeOffset(file.LastWriteTimeUtc).ToUnixTimeMilliseconds());
            }
        }

        // Document ids are "saves", "old", "saves/<file name>" and "old/<file name>".
        string GetPath(string documentId)
        {
            int separatorIndex = documentId.IndexOf('/');
            string folderId = separatorIndex == -1 ? documentId : documentId[..separatorIndex];
            string folder = folderId switch
            {
                SavesFolderId => savesFolder,
                OldSavesFolderId => oldSavesFolder,
                _ => null
            };

            if (folder == null)
                throw new Java.IO.FileNotFoundException(documentId);

            if (separatorIndex == -1)
                return folder;

            string name = documentId[(separatorIndex + 1)..];

            if (name != Path.GetFileName(name))
                throw new Java.IO.FileNotFoundException(documentId);

            return Path.Combine(folder, name);
        }

        static bool IsFile(string documentId) => documentId.Contains('/');

        static string GetParentId(string documentId) => documentId[..documentId.IndexOf('/')];

        static string GetValidFileName(string displayName)
        {
            string name = Path.GetFileName(displayName ?? "");

            foreach (var invalidChar in Path.GetInvalidFileNameChars())
                name = name.Replace(invalidChar, '_');

            if (string.IsNullOrWhiteSpace(name))
                name = "unnamed";

            if (!name.EndsWith(".save", StringComparison.OrdinalIgnoreCase))
                name += ".save";

            return name;
        }

        void NotifyChanged(string folderId)
        {
            Context.ContentResolver.NotifyChange(DocumentsContract.BuildChildDocumentsUri(Authority, folderId), null);
        }
    }
}
