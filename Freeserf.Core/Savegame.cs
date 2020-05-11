/*
 * Savegame.cs - Loading and saving of save games
 *
 * Copyright (C) 2013-2017  Jon Lund Steffensen <jonlst@gmail.com>
 * Copyright (C) 2018-2019  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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

using Freeserf.Data;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Freeserf
{
    using Readers = List<SaveReaderText>;
    using ReaderSections = List<SaveReaderTextSection>;
    using ReaderValues = Dictionary<string, SaveReaderTextValue>;
    using Sections = List<SaveWriterTextSection>;
    using WriterValues = Dictionary<string, SaveWriterTextValue>;

    public class SaveReaderBinary
    {
        BinaryReader reader;
        long size;

        public SaveReaderBinary(SaveReaderBinary reader)
        {
            this.reader = reader.reader;
            size = reader.size;
        }

        public SaveReaderBinary(BinaryReader reader)
        {
            this.reader = reader;
            this.size = reader.BaseStream.Length;
        }

        public byte ReadByte()
        {
            if (!HasDataLeft(1))
                throw new SavegameDataParseException("Invalid read past end.");

            return reader.ReadByte();
        }

        public ushort ReadWord()
        {
            if (!HasDataLeft(2))
                throw new SavegameDataParseException("Invalid read past end.");

            return reader.ReadUInt16();
        }

        public uint ReadDWord()
        {
            if (!HasDataLeft(4))
                throw new SavegameDataParseException("Invalid read past end.");

            return reader.ReadUInt32();
        }

        public void Reset()
        {
            reader.BaseStream.Position = 0;
        }

        public void Skip(uint count)
        {
            reader.BaseStream.Position += count;
        }

        public SaveReaderBinary Extract(uint size)
        {
            if (!HasDataLeft(size))
            {
                throw new SavegameDataParseException("Invalid extract past end.");
            }

            var subStream = new SubStream(this.reader.BaseStream, size);
            var reader = new SaveReaderBinary(new BinaryReader(subStream));

            this.reader.BaseStream.Position += size;

            return reader;
        }

        public byte[] Read(uint size)
        {
            if (!HasDataLeft(size))
                throw new SavegameDataParseException("Invalid read past end.");

            return reader.ReadBytes((int)size);
        }

        public bool HasDataLeft(uint size)
        {
            return reader.BaseStream.Length - reader.BaseStream.Position >= size;
        }
    }

    public class SaveReaderTextValue
    {
        protected string value = "";
        protected List<SaveReaderTextValue> parts = new List<SaveReaderTextValue>();

        public SaveReaderTextValue(string value)
        {
            this.value = value;

            if (value.IndexOf(',') != -1)
            {
                var str = new AutoParseableString(value);
                string item;

                while ((item = str.GetLine(',')) != null)
                {
                    parts.Add(new SaveReaderTextValue(item));
                }
            }
        }

        public int ReadInt()
        {
            return int.Parse(value);
        }

        public uint ReadUInt()
        {
            return uint.Parse(value);
        }

        public bool ReadBool()
        {
            return ReadInt() != 0;
        }

        public long ReadLong()
        {
            return long.Parse(value);
        }

        public Direction ReadDirection()
        {
            return ReadEnum<Direction>();
        }

        public Resource.Type ReadResource()
        {
            return ReadEnum<Resource.Type>();
        }

        public Building.Type ReadBuilding()
        {
            return ReadEnum<Building.Type>();
        }

        public Serf.State ReadSerfState()
        {
            return ReadEnum<Serf.State>();
        }

        dynamic ReadEnum(Type enumType)
        {
            try
            {
                return ReadInt();
            }
            catch
            {
                return Enum.Parse(enumType, value, true);
            }
        }

        public T ReadEnum<T>()
        {
            return (T)ReadEnum(typeof(T));
        }

        public ushort ReadWord()
        {
            return ushort.Parse(value);
        }

        public string ReadString()
        {
            return value;
        }

        public SaveReaderTextValue this[int index] => parts[index];
    }

    public class SaveWriterTextValue
    {
        public string Value { get; protected set; } = "";

        public void Write(object val)
        {
            Value += ((Value.Length > 0) ? "," : "") + val.ToString();
        }

        public void Write(Enum val)
        {
            Write(Enum.GetName(val.GetType(), val));
        }

        public void Write(bool val)
        {
            Value += ((Value.Length > 0) ? "," : "") + (val ? "1" : "0");
        }
    }

    public abstract class SaveReaderText
    {
        public abstract string Name { get; }
        public abstract int Number { get; }

        public abstract SaveReaderTextValue Value(string name);
        public abstract Readers GetSections(string name);
        public abstract bool HasValue(string name);
    }

    public abstract class SaveWriterText
    {
        public abstract string Name { get; }
        public abstract uint Number { get; }

        public abstract SaveWriterTextValue Value(string name);
        public abstract SaveWriterText AddSection(string name, uint number);
    }

    public class GameStore
    {
        public class SaveInfo
        {
            public enum Type
            {
                Legacy,
                Regular
            }

            public string Name = "";
            public string Path = "";
            public Type SaveType = Type.Legacy;

            public override string ToString()
            {
                return Name;
            }
        }

        protected GameStore()
        {
            FolderPath = FileSystem.Paths.SaveGameFolder;

            if (!Directory.Exists(FolderPath))
            {
                try
                {
                    Directory.CreateDirectory(FolderPath);
                }
                catch
                {
                    throw new ExceptionFreeserf(ErrorSystemType.Savegame, "Failed to create folder");
                }
            }
        }

        protected static GameStore instance = null;

        public string FolderPath { get; protected set; } = "";
        protected List<SaveInfo> savedGames = new List<SaveInfo>();

        public enum LastOperationStatus
        {
            None,
            SaveSuccess,
            SaveFail,
            LoadSuccess,
            LoadFail
        }

        public LastOperationStatus LastOperationResult { get; private set; } = LastOperationStatus.None;

        public static GameStore Instance
        {
            get
            {
                if (instance == null)
                    instance = new GameStore();

                return instance;
            }
        }

        public List<SaveInfo> GetSavedGames()
        {
            savedGames.Clear();

            Update();

            return savedGames;
        }

        /* Generic save/load function that will try to detect the right
         format on load and save to the best format on write. */
        public bool Save(string path, Game game)
        {
            path = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            path = path.ReplaceInvalidPathChars('_');

            var writer = new SaveWriterTextSection("game", 0);

            game.WriteTo(writer);

            try
            {
                bool success = writer.Save(path);

                LastOperationResult = success ? LastOperationStatus.SaveSuccess : LastOperationStatus.SaveFail;

                return success;
            }
            catch
            {
                LastOperationResult = LastOperationStatus.SaveFail;

                throw;
            }
        }

        public bool Load(string path, Game game)
        {
            try
            {
                if (!File.Exists(path))
                {
                    LastOperationResult = LastOperationStatus.LoadFail;
                    Log.Error.Write(ErrorSystemType.Savegame, $"Unable to open save game file: '{path}'");
                    return false;
                }

                using (var stream = File.OpenRead(path))
                using (var readerText = new StreamReader(stream, Encoding.ASCII, false, 1024, true))
                using (var readerBinary = new BinaryReader(stream))
                {
                    try
                    {
                        game.ReadFrom(new SaveReaderTextFile(readerText));
                    }
                    catch (ExceptionFreeserf ex1)
                    {
                        readerText.Close();

                        Log.Warn.Write(ErrorSystemType.Savegame, "Unable to load save game: " + ex1.Message);
                        Log.Warn.Write(ErrorSystemType.Savegame, "Trying compatibility mode...");

                        stream.Position = 0;

                        try
                        {
                            game.ReadFrom(new SaveReaderBinary(readerBinary));
                        }
                        catch (ExceptionFreeserf ex2)
                        {
                            LastOperationResult = LastOperationStatus.LoadFail;
                            Log.Error.Write(ErrorSystemType.Savegame, "Failed to load save game: " + ex2.Message);
                            return false;
                        }
                    }

                    for (int i = 0; i < game.PlayerCount; ++i)
                    {
                        var player = game.GetPlayer((uint)i);

                        if (player.HasCastle)
                        {
                            player.CastlePosition = game.GetPlayerBuildings(player, Building.Type.Castle).First().Position;
                        }
                    }

                    LastOperationResult = LastOperationStatus.LoadSuccess;
                    return true;
                }
            }
            catch (Exception ex)
            {
                LastOperationResult = LastOperationStatus.LoadFail;
                Log.Error.Write(ErrorSystemType.Savegame, "Failed to load save game: " + ex.Message);
                return false;
            }
        }

        public bool QuickSave(string prefix, Game game)
        {
            // Build filename including time stamp. 
            var saveGame = new GameStore(); // TODO: Do we need this?
            string name = DateTime.Now.ToString("dd-MM-yy-HH-mm-ss", CultureInfo.InvariantCulture);

            string path = Path.Combine(saveGame.FolderPath, prefix + "-" + name + ".save");

            return Save(path, game);
        }

        public bool QuickSave(string prefix, Game game, out string savedPath)
        {
            // Build filename including time stamp. 
            var saveGame = new GameStore(); // TODO: Do we need this?
            string name = DateTime.Now.ToString("dd-MM-yy-HH-mm-ss", CultureInfo.InvariantCulture);

            savedPath = Path.Combine(saveGame.FolderPath, prefix + "-" + name + ".save");

            return Save(savedPath, game);
        }

        public bool Read(StreamReader reader, Game game)
        {
            try
            {
                var readerText = new SaveReaderTextFile(reader);

                game.ReadFrom(readerText);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool Write(StreamWriter writer, Game game)
        {
            var writerText = new SaveWriterTextSection("game", 0);

            game.WriteTo(writerText);

            return writerText.Write(writer);
        }

        protected void Update()
        {
            FindLegacy();
            FindRegular();
        }

        protected void FindLegacy()
        {
            string archive = Path.Combine(FolderPath + "ARCHIV.DS");

            if (!File.Exists(archive))
                return;

            using (var stream = File.OpenRead(archive))
            using (var reader = new BinaryReader(stream))
            {
                for (int i = 0; i < 10; i++)
                {
                    var name = Encoding.ASCII.GetString(reader.ReadBytes(15));
                    byte used = reader.ReadByte();

                    if (used == 1)
                    {
                        var info = new SaveInfo()
                        {
                            Name = name,
                            Path = Path.Combine(FolderPath, "/SAVE" + ('0' + (char)i) + ".DS"),
                            SaveType = SaveInfo.Type.Legacy
                        };

                        savedGames.Add(info);
                    }
                }
            }
        }

        protected void FindRegular()
        {
            foreach (var file in Directory.GetFiles(FolderPath, "*.save", SearchOption.TopDirectoryOnly))
            {
                var info = new SaveInfo()
                {
                    Name = Path.GetFileNameWithoutExtension(file),
                    Path = file,
                    SaveType = SaveInfo.Type.Regular
                };

                savedGames.Add(info);

            }
        }
    }

    class SaveWriterTextSection : SaveWriterText
    {
        protected string name = "";
        protected uint number;
        protected WriterValues values = new WriterValues();
        protected Sections sections = new Sections();

        public override string Name => name;

        public override uint Number => number;

        public SaveWriterTextSection(string name, uint number)
        {
            this.name = name;
            this.number = number;
        }

        public override SaveWriterTextValue Value(string name)
        {
            if (values.ContainsKey(name))
                return values[name];

            var value = new SaveWriterTextValue();

            values.Add(name, value);

            return value;
        }

        public bool Save(string path)
        {
            var file = new ConfigFile();

            Save(file);

            return file.Save(path);
        }

        public bool Write(StreamWriter writer)
        {
            var file = new ConfigFile();

            Save(file);

            return file.Write(writer);
        }


        public override SaveWriterText AddSection(string subName, uint subNumber)
        {
            var section = new SaveWriterTextSection(subName, subNumber);

            sections.Add(section);

            return section;
        }

        protected void Save(ConfigFile file)
        {
            string str = name + " " + number;

            foreach (var value in values)
            {
                file.SetValue(str, value.Key, value.Value.Value);
            }

            foreach (var writer in sections)
            {
                writer.Save(file);
            }
        }
    }

    class SaveReaderTextSection : SaveReaderText
    {
        protected string name = "";
        protected int number;
        protected ReaderValues values = new ReaderValues();

        public override string Name => name;

        public override int Number => number;

        public SaveReaderTextSection(ConfigFile file, string name)
        {
            this.name = name;
            number = 0;

            var spacePosition = name.IndexOf(' ');

            if (spacePosition != -1)
            {
                string value = name.Substring(spacePosition + 1, name.Length - spacePosition - 1);

                this.name = this.name.Substring(0, spacePosition);

                number = int.Parse(value);
            }

            var vals = file.GetValues(name);

            foreach (string vname in vals)
            {
                if (!values.ContainsKey(vname))
                    values.Add(vname, new SaveReaderTextValue(file.Value(name, vname, "")));
            }
        }

        public override SaveReaderTextValue Value(string name)
        {
            if (!values.ContainsKey(name))
            {
                throw new ExceptionFreeserf(ErrorSystemType.Savegame, "Failed to load value: " + name);
            }

            return values[name];
        }

        public override Readers GetSections(string name)
        {
            throw new ExceptionFreeserf(ErrorSystemType.Savegame, "Recursive sections are not allowed");
        }

        public override bool HasValue(string name)
        {
            return values.ContainsKey(name);
        }
    }

    class SaveReaderTextFile : SaveReaderText
    {
        protected ReaderSections sections = new ReaderSections();
        protected ReaderValues values = new ReaderValues();

        public override string Name => "";

        public override int Number => 0;

        public SaveReaderTextFile(StreamReader reader)
        {
            var file = new ConfigFile();

            file.Read(reader);

            var sections = file.GetSections();

            foreach (string name in sections)
            {
                var section = new SaveReaderTextSection(file, name);

                this.sections.Add(section);
            }

            var values = file.GetValues("main");

            foreach (string vname in values)
            {
                if (!this.values.ContainsKey(vname))
                    this.values.Add(vname, new SaveReaderTextValue(file.Value("main", vname, "")));
            }
        }

        public override SaveReaderTextValue Value(string name)
        {
            if (!values.ContainsKey(name))
            {
                throw new ExceptionFreeserf(ErrorSystemType.Savegame, "Failed to load value: " + name);
            }

            return values[name];
        }

        public override Readers GetSections(string name)
        {
            var result = new Readers();

            foreach (var reader in sections)
            {
                if (reader.Name == name)
                {
                    result.Add(reader);
                }
            }

            return result;
        }

        public override bool HasValue(string name)
        {
            return values.ContainsKey(name);
        }
    }
}
