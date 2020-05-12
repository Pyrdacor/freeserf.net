/*
 * Log.cs - Logging
 *
 * Copyright (C) 2012-2016  Jon Lund Steffensen <jonlst@gmail.com>
 * Copyright (C) 2018-2020  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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

/* Log levels determine the importance of a log message. Use the
   following guidelines when deciding which level to use.

   - ERROR:     The user must take action to continue usage.
                Must usually be followed by an application close.
   - WARN:      Warn the user of an important problem that the
                user might be able to resolve.
   - INFO:      Information that is of general interest to users.
   - DEBUG:     Log a problem that a developer should look into and fix.
   - VERBOSE:   Log any other information that a developer might
                be interested in.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Freeserf
{
    public class LogFileStream : Stream
    {
        readonly string filename;
        FileStream logFileStream;

        public LogFileStream(string filename)
        {
            this.filename = filename;
        }

        public override bool CanRead => false;

        public override bool CanSeek => logFileStream == null ? false : logFileStream.CanSeek;

        public override bool CanWrite => true;

        public override long Length
        {
            get
            {
                EnsureFile();
                return logFileStream.Length;
            }
        }

        public override long Position
        {
            get
            {
                EnsureFile();
                return logFileStream.Position;
            }
            set
            {
                EnsureFile();
                logFileStream.Position = value;
            }
        }

        private void EnsureFile()
        {
            if (logFileStream == null)
                logFileStream = File.Create(filename);
        }

        public override void Flush()
        {
            EnsureFile();
            logFileStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException("Log stream reading is not supported.");
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            EnsureFile();
            return logFileStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            EnsureFile();
            logFileStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            EnsureFile();
            logFileStream.Write(buffer, offset, count);
        }

        public void PopFrontText(int length)
        {
            logFileStream?.Close();

            File.WriteAllText(filename, File.ReadAllText(filename).Substring(length));

            logFileStream = File.OpenWrite(filename);
            logFileStream.Position = logFileStream.Length;
        }
    }

    public class Log
    {
        public enum Level
        {
#if DEBUG
            Verbose = 0,
            Debug,
#endif
            Info = 2,
            Warn,
            Error,

            Max,

#if DEBUG
            Min = Verbose
#else
            Min = Info
#endif
        }

        public class Logger
        {
            protected Level level;
            protected string prefix;
            protected StreamWriter streamWriter;

            public Logger(Level level, string prefix)
            {
                this.level = level;
                this.prefix = prefix;

                ApplyLevel();
            }

            private void PopLine()
            {
                int lineSize = lineSizes.Dequeue();
                (streamWriter.BaseStream as LogFileStream).PopFrontText(lineSize);
                currentSize -= lineSize;
            }

            private void Write(string text)
            {
                if (MaxSize != null)
                {
                    if (MaxSize < text.Length)
                    {
                        Write(text.Substring(0, (int)MaxSize - 3) + "...");
                        return;
                    }

                    while (currentSize + text.Length > MaxSize)
                        PopLine();
                }

                currentSize += text.Length + Environment.NewLine.Length;
                lineSizes.Enqueue(text.Length + Environment.NewLine.Length);
                streamWriter.WriteLine(text);
                streamWriter.Flush();
            }

            public virtual void Write(ErrorSystemType subsystem, string text)
            {
                if (streamWriter == null)
                    return;

                Write($"{prefix}: [{subsystem}] { text}");
            }

            public void ApplyLevel()
            {
                if (level < Log.level)
                {
                    streamWriter = null;
                }
                else
                {
                    streamWriter = stream == null ? null : new StreamWriter(stream, Encoding.UTF8, 1024, true);
                }
            }
        }

        public class NullLogger : Logger
        {
            public NullLogger()
                : base(Level.Max, null)
            {

            }

            public override void Write(ErrorSystemType subsystem, string text)
            {
                // do nothing
            }
        }

        public static void SetStream(Stream stream)
        {
            if (Log.stream == stream)
                return;

            Log.stream = stream;
            currentSize = 0;

            SetLevel(level); // this will attach the streams to the levels
        }

        public static void SetLevel(Level level)
        {
            Log.level = level;

            Verbose.ApplyLevel();
            Debug.ApplyLevel();
            Info.ApplyLevel();
            Warn.ApplyLevel();
            Error.ApplyLevel();
        }

        public static Level LogLevel => level;

#if DEBUG
        public static Logger Verbose = new Logger(Level.Verbose, "Verbose");
        public static Logger Debug = new Logger(Level.Debug, "Debug");
#else
        public static Logger Verbose = new NullLogger();
        public static Logger Debug = new NullLogger();
#endif
        public static Logger Info = new Logger(Level.Info, "Info");
        public static Logger Warn = new Logger(Level.Warn, "Warning");
        public static Logger Error = new Logger(Level.Error, "Error");

        protected static Stream stream = Console.OpenStandardOutput();

#if DEBUG
        protected static Level level = Level.Debug;
#else
        protected static Level level = UserConfig.DefaultLogLevel;
#endif

        static int currentSize = 0;
        static readonly Queue<int> lineSizes = new Queue<int>();
        public static long? MaxSize { get; set; } = UserConfig.DefaultMaxLogSize;
    }
}
