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
using System.IO;
using System.Text;

namespace Freeserf
{
    public class Log
    {
        public enum Level
        {
            Verbose = 0,
            Debug,
            Info,
            Warn,
            Error,

            Max
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

            public virtual void Write(ErrorSystemType subsystem, string text)
            {
                if (streamWriter == null)
                    return;

                streamWriter.Write($"{prefix}: [{subsystem}] ");
                streamWriter.WriteLine(text);
                streamWriter.Flush();
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

        public static void SetStream(Stream stream)
        {
            Log.stream = stream;

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

        public static Logger Verbose = new Logger(Level.Verbose, "Verbose");
        public static Logger Debug = new Logger(Level.Debug, "Debug");
        public static Logger Info = new Logger(Level.Info, "Info");
        public static Logger Warn = new Logger(Level.Warn, "Warning");
        public static Logger Error = new Logger(Level.Error, "Error");

        protected static Stream stream = Console.OpenStandardOutput();

#if DEBUG
        protected static Level level = Level.Debug;
#else
        protected static Level level = Level.Info;
#endif

    }
}
