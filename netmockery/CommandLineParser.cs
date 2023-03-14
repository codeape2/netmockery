using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace netmockery
{
    static public class CommandLineParser
    {
        public const string COMMAND_WEB = "web";
        public const string COMMAND_TEST = "test";
        public const string COMMAND_DUMP = "dump";
        public const string COMMAND_DUMPREFS = "dumprefs";

        private const string VALUE_SWITCH_COMMAND = "--command";
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

        static private string[] COMMAND_VALUES = new[] { COMMAND_WEB, COMMAND_TEST, COMMAND_DUMP, COMMAND_DUMPREFS };
        static private string[] VALUE_SWITCHES = new[] { VALUE_SWITCH_COMMAND, VALUE_SWITCH_ENDPOINTS, VALUE_SWITCH_URLS, VALUE_SWITCH_ONLY, VALUE_UT_SWITCH_ENVIRONMENT, VALUE_UT_SWITCH_CONTENTROOT, VALUE_UT_SWITCH_APPLICATIONNAME };
        static private string[] BOOL_SWITCHES = new[] { BOOL_SWITCH_SHOWRESPONSE, BOOL_SWITCH_NOTESTMODE, BOOL_SWITCH_STOP, BOOL_SWITCH_DIFF, BOOL_SWITCH_LIST };

        static private Dictionary<string, string[]> VALID_SWITHCES_BY_COMMAND = new Dictionary<string, string[]> {

            { COMMAND_WEB, new[] { VALUE_SWITCH_COMMAND, VALUE_SWITCH_ENDPOINTS, VALUE_SWITCH_URLS, BOOL_SWITCH_NOTESTMODE, VALUE_UT_SWITCH_ENVIRONMENT, VALUE_UT_SWITCH_CONTENTROOT, VALUE_UT_SWITCH_APPLICATIONNAME } },
            { COMMAND_TEST, new[] { VALUE_SWITCH_COMMAND, VALUE_SWITCH_ENDPOINTS, VALUE_SWITCH_URLS, VALUE_SWITCH_ONLY, BOOL_SWITCH_SHOWRESPONSE, BOOL_SWITCH_STOP, BOOL_SWITCH_DIFF, BOOL_SWITCH_LIST} },
            { COMMAND_DUMP, new[] { VALUE_SWITCH_COMMAND, VALUE_SWITCH_ENDPOINTS } },
            { COMMAND_DUMPREFS, new[] { VALUE_SWITCH_COMMAND, VALUE_SWITCH_ENDPOINTS } }
        };

        static public ParsedCommandLine ParseArguments(string[] args)
        {
            // Technical debt to support multiple arg input formats, should refactor argument parsing to something standard like System.CommandLine
            // key=value is converted to [key, value]
            args = args
                .Select(arg => arg.ToLower().Split("="))
                .SelectMany(args => args)
                .ToList()
                .ToArray();

            // Parsing
            var seenSwitches = new List<string>();
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
                    seenSwitches.Add(arg);
                }
                else if (BOOL_SWITCHES.Contains(arg))
                {
                    boolValues[arg] = true;
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
            if (switchValues[VALUE_SWITCH_COMMAND] == null)
                throw new CommandLineParsingException($"Missing required switch {VALUE_SWITCH_COMMAND}");
            if (switchValues[VALUE_SWITCH_ENDPOINTS] == null)
                throw new CommandLineParsingException($"Missing required switch {VALUE_SWITCH_ENDPOINTS}");

            var command = switchValues[VALUE_SWITCH_COMMAND];
            
            if (!COMMAND_VALUES.Contains(command))
                throw new CommandLineParsingException($"Unknown command '{command}'");

            foreach (var seenSwitch in seenSwitches)
            {
                if (!VALID_SWITHCES_BY_COMMAND[command].Contains(seenSwitch))
                {
                    throw new CommandLineParsingException($"'{seenSwitch}' is not a valid argument for the '{command}' command");
                }
            }

            // Return
            return new ParsedCommandLine
            {
                Command = command,
                Endpoints = switchValues[VALUE_SWITCH_ENDPOINTS],

                Urls = switchValues[VALUE_SWITCH_URLS],
                Only = switchValues[VALUE_SWITCH_ONLY],

                ShowResponse = boolValues[BOOL_SWITCH_SHOWRESPONSE],
                NoTestMode = boolValues[BOOL_SWITCH_NOTESTMODE],
                Stop = boolValues[BOOL_SWITCH_STOP],
                Diff = boolValues[BOOL_SWITCH_DIFF],
                List = boolValues[BOOL_SWITCH_LIST]
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
