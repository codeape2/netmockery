using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.WindowsServices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static System.Console;

namespace netmockery
{
    public class Program
    {
        private static string _configdirectory;
        public static EndpointCollection EndpointCollection { get; set; }

        public static void ReloadConfig()
        {
            EndpointCollection = EndpointCollectionReader.ReadFromDirectory(_configdirectory);
        }

        private static IWebHost CreateWebHost(string contentRoot)
        {
            return new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(contentRoot)
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();
        }

        public static void Main(string[] args)
        {
            if (IsOutputRedirected)
            {
                OutputEncoding = Encoding.UTF8;
            }
            System.Net.ServicePointManager.ServerCertificateValidationCallback =
                ((sender, certificate, chain, sslPolicyErrors) => true);
            if (args.Length >= 1)
            {
                Debug.Assert(Directory.Exists(args[0]));
                _configdirectory = args[0];
                ReloadConfig();

                if (args.Length == 1)
                {
                    WriteLine("Admin interface available on /__netmockery");
                    CreateWebHost(Directory.GetCurrentDirectory()).Run();
                }
                else
                {
                    Debug.Assert(args.Length > 1);
                    var commandName = args[1];
                    var commandArgs = args.Skip(2).ToArray();
                    switch (commandName)
                    {
                        case "match":
                            Match(commandArgs);
                            break;

                        case "dump":
                            Dump(commandArgs);
                            break;

                        case "runscript":
                            RunScript(commandArgs);
                            break;

                        case "viewscript":
                            ViewScript(commandArgs);
                            break;

                        case "test":
                            Test(commandArgs);
                            break;

                        case "service":
                            WriteLine("Running as service");
                            RunAsService();
                            break;

                        default:
                            Error.WriteLine($"Unknown command {commandName}");
                            break;
                    }
                }
            }
            else
            {
                Error.WriteLine("Configuration directory not specified");
            }
        }

        static public void RunAsService()
        {
            CreateWebHost(AppDomain.CurrentDomain.BaseDirectory).RunAsService();
        }

        static private string getSwitchValue(string[] commandArgs, string switchName)
        {
            var index = Array.FindIndex(commandArgs, v => v == switchName);
            if (index == -1)
            {
                return null;
            }
            else
            {
                return commandArgs[index + 1];
            }
        }

        static private bool containsSwitch(string[] commandArgs, string switchName)
        {
            Debug.Assert(commandArgs != null);
            Debug.Assert(switchName != null);
            return commandArgs.Contains(switchName);
        }


        public static void Test(string[] commandArgs)
        {
            if (TestRunner.HasTestSuite(EndpointCollection.SourceDirectory))
            {
                var testRunner = new TestRunner(EndpointCollection);
                var only = getSwitchValue(commandArgs, "--only");
                if (only != null)
                {
                    var index = int.Parse(only);
                    if (containsSwitch(commandArgs, "--showResponse"))
                    {
                        testRunner.ShowResponse(index);
                    }
                    else
                    {
                        testRunner.ExecuteTestAndOutputResult(index);
                    }
                }
                else
                {
                    testRunner.TestAll();
                }                
            }
            else
            {
                Error.WriteLine("ERROR: No test suite found");
            }
        }

        public static void RunScript(string[] commandArgs)
        {
            var scriptfile = commandArgs[0];

            var responseCreator = new FileDynamicResponseCreator(scriptfile);
            var body = responseCreator.GetBody(new RequestInfo {
                RequestBody = commandArgs.Length == 2 ? File.ReadAllText(commandArgs[1]) : "",
                EndpointDirectory = Path.GetDirectoryName(scriptfile)
            });
            Console.WriteLine(body);
        }

        public static void ViewScript(string[] commandArgs)
        {
            var scriptfile = commandArgs[0];
            Console.WriteLine(DynamicResponseCreatorBase.ExecuteIncludes(File.ReadAllText(scriptfile), Path.GetDirectoryName(scriptfile)));
        }

        public static void Match(string[] args)
        {
            var path = args[0];
            var body = File.ReadAllText(args[1]);
            
            var endpoint = EndpointCollection.Resolve(path);
            if (endpoint == null)
            {
                WriteLine("No endpoint match");
                return;
            }
            WriteLine($"Endpoint: {endpoint.Name}");
            bool singleMatch;
            var responseMatch = endpoint.Resolve(new Microsoft.AspNetCore.Http.PathString(path), new Microsoft.AspNetCore.Http.QueryString(), body, null, out singleMatch);
            if (responseMatch == null)
            {
                WriteLine("No match");
                return;
            }
            WriteLine(responseMatch);
        }

        public static void Dump(string[] args)
        {
            Debug.Assert(args.Length == 0);
            foreach (var endpoint in EndpointCollection.Endpoints)
            {
                WriteLine($"{endpoint.Name} {endpoint.PathRegex}");
                foreach (var response in endpoint.Responses)
                {
                    WriteLine($"    {response.Item1} -> {response.Item2}");
                }
            }
        }
    }
}
