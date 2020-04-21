/*
 * CommandLine.cs - Command line parser and helpers
 *
 * Copyright (C) 2017  Wicked_Digger <wicked_digger@mail.ru>
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

using Freeserf.Data;
using System;
using System.Collections.Generic;

namespace Freeserf
{
    public class CommandLine
    {
        const int CommentRowOffset = 14;
        const int MaxCommentLineLength = 60;

        public class Option
        {
            protected struct Parameter
            {
                public string Name;
                public Func<AutoParseableString, bool> Handler;
                public string PossibleValues;
            }

            protected string comment = "";
            protected Action handler;
            protected List<Parameter> parameters = new List<Parameter>();

            public Option()
            {
            }

            public Option(string comment, Action handler = null)
            {
                this.comment = comment;

                if (handler == null)
                    this.handler = () => { };
                else
                    this.handler = handler;
            }

            public bool HasParameter()
            {
                return parameters.Count > 0;
            }

            public string Comment => comment;

            private static string SplitToLines(string text, int newLineOffset, int additionalLineIdentation = 0)
            {
                int maxLength = MaxCommentLineLength;

                string lines = "";
                int offset = 0;

                while (offset < text.Length)
                {
                    string remaining = text.Substring(offset);
                    string line = remaining.Substring(0, Math.Min(maxLength, remaining.Length));

                    if (line.Contains("\n"))
                    {
                        int newLinePos = line.IndexOf('\n');
                        line = line.Substring(0, newLinePos);
                    }
                    else if (remaining.Length > maxLength)
                    {
                        int lastSpace = line.LastIndexOf(' ');

                        if (lastSpace == -1) // not splitable
                            return text;

                        line = line.Substring(0, lastSpace);
                    }
                    string prefix = offset == 0 ? "" : "\t" + new string(' ', newLineOffset + additionalLineIdentation);
                    if (offset == 0)
                        maxLength -= additionalLineIdentation;
                    lines += prefix + line.TrimEnd() + Environment.NewLine;
                    offset += line.Length + 1;
                }

                return lines;
            }

            public void ShowHelp()
            {
                string possibleValues = "";

                if (parameters.Count > 0)
                {
                    int length = 0;

                    foreach (var par in parameters)
                    {
                        Cout.Write(" " + par.Name);
                        length += par.Name.Length + 1;

                        if (par.PossibleValues != null)
                            possibleValues += SplitToLines("  " + par.Name + ": " + par.PossibleValues, 2 + CommentRowOffset, par.Name.Length + 2) + Environment.NewLine;
                    }

                    if (length < CommentRowOffset)
                        Cout.Write(new string(' ', CommentRowOffset - length));
                    else
                        Cout.Write(" ");
                }
                else
                {
                    Cout.Write(new string(' ', CommentRowOffset));
                }

                Cout.Write(SplitToLines(comment, 2 + CommentRowOffset).TrimEnd());

                if (possibleValues.Length > 0)
                    Cout.Write(Environment.NewLine + "\t" + new string(' ', CommentRowOffset) + possibleValues.TrimEnd());
            }

            public void ShowUsage()
            {
                foreach (var par in parameters)
                {
                    Cout.Write(" " + par.Name);
                }
            }

            public Option AddParameter(string name, Func<AutoParseableString, bool> handler, string possibleValues = null)
            {
                var parameter = new Parameter();

                parameter.Name = name;
                parameter.Handler = handler;
                parameter.PossibleValues = possibleValues;
                parameters.Add(parameter);

                return this;
            }

            public bool Process(List<string> parameters)
            {
                handler();

                if (parameters.Count != this.parameters.Count)
                {
                    return false;
                }

                for (var i = 0; i < parameters.Count; ++i)
                {
                    if (!this.parameters[i].Handler(new AutoParseableString(parameters[i])))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        protected string path = "";
        protected string programName = "";
        protected string comment = "";
        readonly Dictionary<char, Option> options = new Dictionary<char, Option>();

        public bool Process(string[] args)
        {
            Queue<string> arguments = new Queue<string>(args.Length);

            foreach (var arg in args)
                arguments.Enqueue(arg);

            while (arguments.Count > 0)
            {
                string arg = arguments.Dequeue();

                if ((arg.Length != 2) || (arg[0] != '-'))
                {
                    Cout.WriteLine($"Unknown command line parameter: \"{arg}\"");
                    continue;
                }

                char key = arg[1];

                if (!options.ContainsKey(key))
                {
                    ShowHelp();
                    return false;
                }

                Option opt = options[key];

                List<string> parameters = new List<string>();

                while (arguments.Count > 0 && (arguments.Peek()[0] != '-'))
                {
                    parameters.Add(arguments.Dequeue());
                }

                if (!opt.Process(parameters))
                {
                    ShowHelp();
                    return false;
                }
            }

            return true;
        }

        public Option AddOption(char key, string comment, Action handler = null)
        {
            options[key] = new Option(comment, handler);

            return options[key];
        }

        public string Path => path;

        public string ProgramName => programName;

        public void SetComment(string comment)
        {
            this.comment = comment;
        }

        public void ShowHelp()
        {
            ShowUsage();

            Cout.WriteLine();

            foreach (var opt in options)
            {
                Cout.Write("\t-" + opt.Key);
                opt.Value.ShowHelp();
                Cout.WriteLine();
            }

            if (!string.IsNullOrWhiteSpace(comment))
            {
                Cout.WriteLine();
                Cout.WriteLine(comment);
            }
        }

        public void ShowUsage()
        {
            Cout.Write("Usage: " + path);

            foreach (var opt in options)
            {
                Cout.Write(" [-" + opt.Key);
                opt.Value.ShowUsage();
                Cout.Write("]");
            }

            Cout.WriteLine();
        }
    }
}
