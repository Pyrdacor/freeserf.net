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
using System.Runtime.CompilerServices;

namespace Freeserf
{
    public class ExceptionFreeserf : Exception
    {
        public string Description
        {
            get;
            protected set;
        }

        public virtual string System => "Unspecified";

        public ExceptionFreeserf(string description)
        {
            Description = description;
        }

        public override string ToString()
        {
            return Message;
        }

        public override string Message => "[" + System + "]" + Description;
    }

    public static class Debug
    {
        static string SourceCodeLocation([CallerLineNumber] int lineNumber = 0,
            [CallerFilePath] string file = "")
        {
            return " at line " + lineNumber + " of " + file + ".";
        }

#if DEBUG
        public static void NotReached()
        {
            Log.Error.Write("debug", "NOT_REACHED" + SourceCodeLocation());
            Environment.Exit(6);
        }
#else
        public static void NotReached()
        {
        }
#endif

    };

}
