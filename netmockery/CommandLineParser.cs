using System;
using System.Collections.Generic;
using System.Linq;

namespace netmockery
{
    static public class CommandLineParser
    {
        public const string COMMAND_WEB = "web";
        public const string COMMAND_TEST = "test";
        public const string COMMAND_DUMP = "dump";
        public const string COMMAND_DUMPREFS = "dumprefs";

        private const string VALUE_SWITCH_ENDPOINTS = "--endpoints";
        private const string VALUE_SWITCH_URLS = "--urls";
        private const string VALUE_SWITCH_ONLY = "--only";

        private const string VALUE_UT_SWITCH_ENVIRONMENT = "--environment";
        private const string VALUE_UT_SWITCH_CONTENTROOT = "--contentroot";
        private const string VALUE_UT_SWITCH_APPLICATIONNAME = "--applicationname";

        private const string BOOL_SWITCH_SHOWRESPONSE = "--showresponse";
        private const string BOOL_SWITCH_NOTESTMODE = "--notestmode";
        private const string BOOL_SWITCH_STOP = "--stop";
        private const string BOOL_SWITCH_DIFF = "--diff";
        private const string BOOL_SWITCH_LIST = "--list";

        static private string[] VALUE_SWITCHES = new[] { VALUE_SWITCH_ENDPOINTS, VALUE_SWITCH_URLS, VALUE_SWITCH_ONLY, VALUE_UT_SWITCH_ENVIRONMENT, VALUE_UT_SWITCH_CONTENTROOT, VALUE_UT_SWITCH_APPLICATIONNAME };
        static private string[] BOOL_SWITCHES = new[] { BOOL_SWITCH_SHOWRESPONSE, BOOL_SWITCH_NOTESTMODE, BOOL_SWITCH_STOP, BOOL_SWITCH_DIFF, BOOL_SWITCH_LIST };

        static private Dictionary<string, string[]> VALID_SWITHCES_BY_COMMAND = new Dictionary<string, string[]> {

            { COMMAND_WEB, new[] { VALUE_SWITCH_ENDPOINTS, VALUE_SWITCH_URLS, BOOL_SWITCH_NOTESTMODE, VALUE_UT_SWITCH_ENVIRONMENT, VALUE_UT_SWITCH_CONTENTROOT, VALUE_UT_SWITCH_APPLICATIONNAME } },
            { COMMAND_TEST, new[] { VALUE_SWITCH_ENDPOINTS, VALUE_SWITCH_URLS, VALUE_SWITCH_ONLY, BOOL_SWITCH_SHOWRESPONSE, BOOL_SWITCH_STOP, BOOL_SWITCH_DIFF, BOOL_SWITCH_LIST} },
            { COMMAND_DUMP, new[] { VALUE_SWITCH_ENDPOINTS } },
            { COMMAND_DUMPREFS, new[] { VALUE_SWITCH_ENDPOINTS } }
        };

        static public ParsedCommandLine ParseArguments(string[] args)
        {
            if (args.Length == 0)
                throw new CommandLineParsingException("No arguments");

            // Technical debt to support multiple arg input formats, should refactor argument parsing to something standard like System.CommandLine
            // key=value is converted to [key, value]
            args = args
                .Select(arg => arg.ToLower().Split("="))
                .SelectMany(args => args)
                .ToList()
                .ToArray();

            // Parsing
            (var command, args) = ParseCommand(args);
            (var stringSwiches, var boolSwitches) = ParseSwitches(command, args);

            // Return
            return new ParsedCommandLine
            {
                Command = command,
                Endpoints = stringSwiches[VALUE_SWITCH_ENDPOINTS],

                Urls = stringSwiches[VALUE_SWITCH_URLS],
                Only = stringSwiches[VALUE_SWITCH_ONLY],
                
                ShowResponse = boolSwitches[BOOL_SWITCH_SHOWRESPONSE],
                NoTestMode = boolSwitches[BOOL_SWITCH_NOTESTMODE],
                Stop = boolSwitches[BOOL_SWITCH_STOP],
                Diff = boolSwitches[BOOL_SWITCH_DIFF],
                List = boolSwitches[BOOL_SWITCH_LIST]
            };
        }

        static private (string Command, string[] RemaingArgs) ParseCommand(string[] args)
        {
            string first = args.First();

            if (first == COMMAND_WEB)
            {
                return (COMMAND_WEB, args.Skip(1).ToArray());
            }
            else if (first == COMMAND_TEST)
            {
                return (COMMAND_TEST, args.Skip(1).ToArray());
            }
            else if (first == COMMAND_DUMP)
            {
                return (COMMAND_DUMP, args.Skip(1).ToArray());
            }
            else if (first == COMMAND_DUMPREFS)
            {
                return (COMMAND_DUMPREFS, args.Skip(1).ToArray());
            }
            else if (!first.StartsWith("--"))
            {
                throw new CommandLineParsingException($"Unknown command '{first}'");
            }
            else
            {
                return (COMMAND_WEB, args.ToArray());
            }
        }

        static private (Dictionary<string, string> StringSwitches, Dictionary<string, bool> BoolSwitches) ParseSwitches(string command, string[] args)
        {
            var seenSwitches = new List<string>();
            var stringSwitches = new Dictionary<string, string>();
            var boolSwitches = new Dictionary<string, bool>();

            foreach (var valueSwitch in VALUE_SWITCHES)
            {
                stringSwitches[valueSwitch] = null;
            }

            foreach (var boolSwitch in BOOL_SWITCHES)
            {
                boolSwitches[boolSwitch] = false;
            }

            var i = 0;
            while (i < args.Length)
            {
                var arg = args[i];
                if (VALUE_SWITCHES.Contains(arg))
                {
                    var value = args[++i];
                    stringSwitches[arg] = value;
                    seenSwitches.Add(arg);
                }
                else if (BOOL_SWITCHES.Contains(arg))
                {
                    boolSwitches[arg] = true;
                    seenSwitches.Add(arg);
                }
                else if (arg.StartsWith("--"))
                {
                    throw new CommandLineParsingException($"Unknown switch '{arg}'");
                }
                else
                {
                    throw new CommandLineParsingException($"Positional arguments not supported, got '{arg}'");
                }
                i++;
            }

            // Validation
            if (stringSwitches[VALUE_SWITCH_ENDPOINTS] == null)
                throw new CommandLineParsingException($"Missing required switch {VALUE_SWITCH_ENDPOINTS}");

            foreach (var seenSwitch in seenSwitches)
            {
                if (!VALID_SWITHCES_BY_COMMAND[command].Contains(seenSwitch))
                {
                    throw new CommandLineParsingException($"'{seenSwitch}' is not a valid argument for the '{command}' command");
                }
            }

            return (stringSwitches, boolSwitches);
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
        public string Command;
        public string Endpoints;

        public string Urls;
        public string Only;

        public bool ShowResponse;
        public bool NoTestMode;
        public bool Stop;
        public bool Diff;
        public bool List;

        public bool TestMode => !NoTestMode;
    }
}
