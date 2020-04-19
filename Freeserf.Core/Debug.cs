/*
 * Debug.cs - Definitions to ease debugging.
 *
 * Copyright (C) 2012  Jon Lund Steffensen <jonlst@gmail.com>
 * Copyright (C) 2018  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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
using System.IO;
using System.Runtime.CompilerServices;

namespace Freeserf
{
    public enum ErrorSystemType
    {
        None,
        Config,
        Data,
        Game,
        Map,
        Render,
        Textures,
        Savegame,
        Building,
        Flag,
        Serf,
        Player,
        Inventory,
        AI,
        UI,
        Audio,
        Application,
        Network
    }

    public class ExceptionFreeserf : Exception
    {
        public string Description
        {
            get;
            protected set;
        }

        public virtual ErrorSystemType System { get; private set; } = ErrorSystemType.None;

        public string SourceFile { get; }
        public int SourceLineNumber { get; }
        public Game Game { get; } = null;

        public ExceptionFreeserf(string description, [CallerLineNumber] int lineNumber = 0,
            [CallerFilePath] string file = "")
        {
            Description = description;
            SourceFile = file;
            SourceLineNumber = lineNumber;
        }

        public ExceptionFreeserf(ErrorSystemType system, string description,
            [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string file = "")
        {
            System = system;
            Description = description;
            SourceFile = file;
            SourceLineNumber = lineNumber;
        }

        public ExceptionFreeserf(Game game, string description, [CallerLineNumber] int lineNumber = 0,
            [CallerFilePath] string file = "")
        {
            Description = description;
            SourceFile = file;
            SourceLineNumber = lineNumber;
            Game = game;
        }

        public ExceptionFreeserf(Game game, ErrorSystemType system, string description,
            [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string file = "")
        {
            System = system;
            Description = description;
            SourceFile = file;
            SourceLineNumber = lineNumber;
            Game = game;
        }

        public ExceptionFreeserf(Game game, Exception inner, [CallerLineNumber] int lineNumber = 0,
            [CallerFilePath] string file = "")
            : base(inner.Message, inner)
        {
            if (inner is ExceptionFreeserf)
                System = (inner as ExceptionFreeserf).System;

            Description = inner.Message;
            SourceFile = file;
            SourceLineNumber = lineNumber;
            Game = game;
        }

        public ExceptionFreeserf(Game game, ErrorSystemType system, Exception inner, [CallerLineNumber] int lineNumber = 0,
            [CallerFilePath] string file = "")
            : base(inner.Message, inner)
        {
            if (inner is ExceptionFreeserf)
            {
                System = (inner as ExceptionFreeserf).System;
                Description = (inner as ExceptionFreeserf).Description;
            }
            else
            {
                System = system;
                Description = inner.Message;
            }

            SourceFile = file;
            SourceLineNumber = lineNumber;
            Game = game;
        }

        public override string ToString()
        {
            return Message;
        }

        public override string Message => System == ErrorSystemType.None ? Description : "[" + System.ToString() + "] " + Description;

        public override string StackTrace => InnerException == null ? base.StackTrace : InnerException.StackTrace;
    }

    public static class Debug
    {

#if DEBUG
        public static void NotReached([CallerLineNumber] int lineNumber = 0,
            [CallerFilePath] string file = "")
        {
            Log.Error.Write(ErrorSystemType.Application, $"NOT_REACHED at line {lineNumber} of {Path.GetFileName(file)}.");
            throw new ExceptionFreeserf(ErrorSystemType.Application, "Not reached");
        }
#else
        public static void NotReached()
        {
        }
#endif

    };

}
