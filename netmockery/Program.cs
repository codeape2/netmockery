using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        public static void Main(string[] args)
        {
            System.Net.ServicePointManager.ServerCertificateValidationCallback =
                ((sender, certificate, chain, sslPolicyErrors) => true);
            if (args.Length >= 1)
            {
                Debug.Assert(Directory.Exists(args[0]));
                _configdirectory = args[0];
                ReloadConfig();

                if (args.Length == 1)
                {
                    var host = new WebHostBuilder()
                        .UseKestrel()
                        .UseContentRoot(Directory.GetCurrentDirectory())
                        .UseIISIntegration()
                        .UseStartup<Startup>()
                        .Build();

                    WriteLine("Admin interface available on /__netmockery");
                    host.Run();
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


        public static void RunScript(string[] commandArgs)
        {
            var scriptfile = commandArgs[0];

            var responseCreator = new FileDynamicResponseCreator(scriptfile);
            var body = responseCreator.GetBody(new RequestInfo { EndpointDirectory = Path.GetDirectoryName(scriptfile) });
            Console.WriteLine(body);
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
            var responseMatch = endpoint.Resolve(new Microsoft.AspNetCore.Http.PathString(path), body, null, out singleMatch);
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
