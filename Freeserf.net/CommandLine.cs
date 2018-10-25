using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Freeserf
{
    public class CommandLine
    {
        public class Option
        {
            protected struct Parameter
            {
                public string Name;
                public Func<StringBuilder, bool> Handler;
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

            public string GetComment()
            {
                return comment;
            }

            public void ShowHelp()
            {
                if (parameters.Count > 0)
                {
                    foreach (var par in parameters)
                    {
                        Cout.Write(" " + par.Name + "\t");
                    }
                }
                else
                {
                    Cout.Write("\t\t");
                }

                Cout.Write(comment);
            }

            public void ShowUsage()
            {
                foreach (var par in parameters)
                {
                    Cout.Write(" " + par.Name);
                }
            }

            public Option AddParameter(string name, Func<StringBuilder, bool> handler)
            {
                var parameter = new Parameter();

                parameter.Name = name;
                parameter.Handler = handler;
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
                    var s = new StringBuilder();

                    s.Append(parameters[i]);

                    if (!this.parameters[i].Handler(s))
                    {
                      return false;
                    }
                }
                return true;
            }
        }

        protected string path = "";
        protected string progname = "";
        protected string comment = "";
        Dictionary<char, Option> options = new Dictionary<char, Option>();

        public bool Process(string[] args2)
        {
            Queue<string> arguments = new Queue<string>(args2.Length);

            foreach (var arg in args2)
                arguments.Enqueue(arg);

            if (arguments.Count < 1)
            {
                return false;
            }

            path = arguments.Dequeue();

            var pos = path.LastIndexOf(@"\/");

            if (pos != -1)
            {
                progname = path.Substring(pos + 1, path.Length - pos - 1);
            }
            else
            {
                progname = path;
            }

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

        public string GetPath()
        {
            return path;
        }

        public string GetProgName()
        {
            return progname;
        }

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
