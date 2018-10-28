/*
 * Savegame.cs - Loading and saving of save games
 *
 * Copyright (C) 2013-2017  Jon Lund Steffensen <jonlst@gmail.com>
 * Copyright (C) 2018       Robert Schneckenhaus <robert.schneckenhaus@web.de>
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Globalization;

namespace Freeserf
{
    using Readers = List<SaveReaderText>;
    using ReaderValues = Dictionary<string, SaveReaderTextValue>;
    using WriterValues = Dictionary<string, SaveWriterTextValue>;
    using Sections = List<SaveWriterTextSection>;
    using ReaderSections = List<SaveReaderTextSection>;

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
                throw new ExceptionFreeserf("Invalid read past end.");

            return reader.ReadByte();
        }

        public ushort ReadWord()
        {
            if (!HasDataLeft(2))
                throw new ExceptionFreeserf("Invalid read past end.");

            return reader.ReadUInt16();
        }

        public uint ReadDWord()
        {
            if (!HasDataLeft(4))
                throw new ExceptionFreeserf("Invalid read past end.");

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
                throw new ExceptionFreeserf("Invalid extract past end.");
            }

            var subStream = new SubStream(this.reader.BaseStream, size);
            var reader = new SaveReaderBinary(new BinaryReader(subStream));

            this.reader.BaseStream.Position += size;

            return reader;
        }

        public byte[] Read(uint size)
        {
            if (!HasDataLeft(size))
                throw new ExceptionFreeserf("Invalid read past end.");

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
                var s = new AutoParseableString(value);
                string item;

                while ((item = s.GetLine(',')) != null)
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

        public Direction ReadDirection()
        {
            return (Direction)ReadInt();
        }

        public Resource.Type ReadResource()
        {
            return (Resource.Type)ReadInt();
        }

        public Building.Type ReadBuilding()
        {
            return (Building.Type)ReadInt();
        }

        public Serf.State ReadSerfState()
        {
            return (Serf.State)ReadInt();
        }

        public ushort ReadWord()
        {
            return ushort.Parse(value);
        }

        public string ReadString()
        {
            return value;
        }

        public SaveReaderTextValue this[uint pos] => parts[(int)pos];
    }

    public class SaveWriterTextValue
    {
        public string Value { get; protected set; } = "";

        public static SaveWriterTextValue operator +(SaveWriterTextValue v1, object v2)
        {
            return new SaveWriterTextValue()
            {
                Value = v1.Value + ((v1.Value.Length > 0) ? "," : "") + v2.ToString()
            };
        }

        public static SaveWriterTextValue operator +(SaveWriterTextValue v1, Enum v2)
        {
            return new SaveWriterTextValue()
            {
                Value = v1.Value + ((v1.Value.Length > 0) ? "," : "") + Enum.GetName(v2.GetType(), v2)
            };
        }
    }

    public abstract class SaveReaderText
    {
        public abstract string Name { get; }
        public abstract int Number { get; }

        public abstract SaveReaderTextValue Value(string name);
        public abstract Readers GetSections(string name);
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
            public string path = "";
            public Type type = Type.Legacy;
        }

        protected GameStore()
        {
            FolderPath = ".";

#if WINDOWS
            FolderPath = Path.Combine(Environment.GetEnvironmentVariable("USERPROFILE"), "Saved Games");
#elif __APPLE__
            FolderPath = Environment.GetEnvironmentVariable("HOME");
            FolderPath += "/Library/Application Support";
#else
            FolderPath = Environment.GetEnvironmentVariable("HOME");
            FolderPath += "/.local/share";
#endif

            FolderPath += "/freeserf";

#if !WINDOWS
            FolderPath += "/saves";

            if (!is_folder_exists(folder_path))
            {
                if (!create_folder(folder_path))
                {
                    throw ExceptionFreeserf("Failed to create folder");
                }
            }
#endif

            if (!Directory.Exists(FolderPath))
            {
                try
                {
                    Directory.CreateDirectory(FolderPath);
                }
                catch
                {
                    throw new ExceptionFreeserf("Failed to create folder");
                }
            }
        }

        protected static GameStore instance = null;

        public string FolderPath { get; protected set; } = "";
        protected List<SaveInfo> savedGames = new List<SaveInfo>();

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
            path = path.ReplaceInvalidPathChars('_');

            var writer = new SaveWriterTextSection("game", 0);

            game.WriteTo(writer);

            return writer.Save(path);
        }

        public bool Load(string path, Game game)
        {
            if (!File.Exists(path))
            {
                Log.Error.Write("savegame", $"Unable to open save game file: '{path}'");
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
                    readerBinary.Close();

                    Log.Warn.Write("savegame", "Unable to load save game: " + ex1.Message);
                    Log.Warn.Write("savegame", "Trying compatability mode...");

                    stream.Position = 0;

                    try
                    {
                        game.ReadFrom(new SaveReaderBinary(readerBinary));
                    }
                    catch (ExceptionFreeserf ex2)
                    {
                        Log.Error.Write("savegame", "Failed to load save game: " + ex2.Message);
                        return false;
                    }
                }

                return true;
            }
        }

        public bool QuickSave(string prefix, Game game)
        {
            /* Build filename including time stamp. */
            GameStore save_game = new GameStore();
            string name = DateTime.Now.ToString("dd-MM-yy_HH-mm-ss", CultureInfo.InvariantCulture);

            string path = Path.Combine(save_game.FolderPath, prefix + "-" + name + ".save");

            return Save(path, game);
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
                            path = Path.Combine(FolderPath, "/SAVE" + ('0' + (char)i) + ".DS"),
                            type = SaveInfo.Type.Legacy
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
                    path = Path.GetDirectoryName(file),
                    type = SaveInfo.Type.Regular
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
            ConfigFile file = new ConfigFile();

            Save(file);

            return file.Save(path);
        }

        public bool Write(StreamWriter writer)
        {
            ConfigFile file = new ConfigFile();

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

            foreach (SaveWriterTextSection writer in sections)
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

            var pos = name.IndexOf(' ');

            if (pos != -1)
            {
                string value = name.Substring(pos + 1, name.Length - pos - 1);

                name = name.Substring(0, pos);

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
                throw new ExceptionFreeserf("Failed to load value: " + name);
            }

            return values[name];
        }

        public override Readers GetSections(string name)
        {
            throw new ExceptionFreeserf("Recursive sections are not allowed");
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
            ConfigFile file = new ConfigFile();

            file.Read(reader);

            var sects = file.GetSections();

            foreach (string name in sects)
            {
                var section = new SaveReaderTextSection(file, name);

                sections.Add(section);
            }

            var vals = file.GetValues("main");

            foreach (string vname in vals)
            {
                if (!values.ContainsKey(vname))
                    values.Add(vname, new SaveReaderTextValue(file.Value("main", vname, "")));
            }
        }

        public override SaveReaderTextValue Value(string name)
        {
            if (!values.ContainsKey(name))
            {
                throw new ExceptionFreeserf("Failed to load value: " + name);
            }

            return values[name];
        }

        public override Readers GetSections(string name)
        {
            Readers result = new Readers();

            foreach (SaveReaderTextSection reader in sections)
            {
                if (reader.Name == name)
                {
                    result.Add(reader);
                }
            }

            return result;
        }
    }
}
