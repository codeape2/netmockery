using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace netmockery
{
    static public class CommandLineParser
    {
        public const int COMMAND_NORMAL = 1;
        public const int COMMAND_SERVICE = 2;
        public const int COMMAND_TEST = 3;
        public const int COMMAND_DUMP = 4;

        private const string VALUE_SWITCH_URL = "--url";
        private const string VALUE_SWITCH_ONLY = "--only";

        private const string BOOL_SWITCH_SHOWRESPONSE = "--showResponse";
        private const string BOOL_SWITCH_NOTESTMODE = "--notestmode";

        static private string[] VALUE_SWITCHES = new[] { VALUE_SWITCH_URL, VALUE_SWITCH_ONLY };
        static private string[] BOOL_SWITCHES = new[] { BOOL_SWITCH_SHOWRESPONSE, BOOL_SWITCH_NOTESTMODE };


        static public ParsedCommandLine ParseArguments(string[] args)
        {
            var positionalArgs = new List<string>();
            var switchValues = new Dictionary<string, string>();
            var boolValues = new Dictionary<string, bool>();

            foreach (var valueSwitch in VALUE_SWITCHES)
            {
                switchValues[valueSwitch] = null;
            }

            foreach (var boolSwitch in BOOL_SWITCHES)
            {
                boolValues[boolSwitch] = false;
            }

            var i = 0;
            while (i < args.Length)
            {
                var arg = args[i];
                if (VALUE_SWITCHES.Contains(arg))
                {
                    var value = args[++i];
                    switchValues[arg] = value;

                    if (arg == VALUE_SWITCH_ONLY)
                    {
                        int dummy;
                        if (! int.TryParse(value, out dummy))
                        {
                            throw new CommandLineParsingException($"Argument --only: integer required");
                        }
                    }
                }
                else if (BOOL_SWITCHES.Contains(arg))
                {
                    boolValues[arg] = true;
                }
                else if (arg.StartsWith("--"))
                {
                    throw new CommandLineParsingException($"Unknown switch '{arg}'");
                }
                else
                {
                    positionalArgs.Add(arg);
                }
                i++;
            }

            if (positionalArgs.Count == 0)
            {
                throw new CommandLineParsingException("No endpoint directory specified");
            }

            if (positionalArgs.Count > 2)
            {
                throw new CommandLineParsingException($"Unexpected number ({positionalArgs.Count}) of positional arguments.");
            }

            Debug.Assert(positionalArgs.Count == 1 || positionalArgs.Count == 2);
            
            var command = COMMAND_NORMAL;
            if (positionalArgs.Count == 2)
            {
                switch (positionalArgs.ElementAt(1))
                {
                    case "service":
                        command = COMMAND_SERVICE;
                        break;

                    case "test":
                        command = COMMAND_TEST;
                        break;

                    case "dump":
                        command = COMMAND_DUMP;
                        break;

                    default:
                        throw new CommandLineParsingException($"Unknown command '{positionalArgs.ElementAt(1)}'");
                }
            }
            return new ParsedCommandLine
            {
                Command = command,
                EndpointCollectionDirectory = positionalArgs.ElementAt(0),

                Url = switchValues[VALUE_SWITCH_URL],
                Only = switchValues[VALUE_SWITCH_ONLY],

                ShowResponse = boolValues[BOOL_SWITCH_SHOWRESPONSE],
                NoTestMode = boolValues[BOOL_SWITCH_NOTESTMODE],
            };
        }
    }


    public class CommandLineParsingException : Exception
    {
        public CommandLineParsingException(string message) : base(message)
        {
        }
    }


    public class ParsedCommandLine
    {
        public int Command;
        public string EndpointCollectionDirectory;

        public string Url;
        public string Only;

        public bool ShowResponse;
        public bool NoTestMode;

        public bool TestMode => !NoTestMode;
    }
}
