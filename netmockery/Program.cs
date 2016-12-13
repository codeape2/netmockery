using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.WindowsServices;
using Microsoft.Extensions.DependencyInjection;
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
        public static IWebHost CreateWebHost(string endpointCollectionDirectory, string contentRoot, string url)
        {
            var endpointCollectionProvider = new EndpointCollectionProvider(endpointCollectionDirectory);
            Action<IServiceCollection> initialSvcConfig = (IServiceCollection serviceCollection) =>
            {
                serviceCollection.AddTransient(serviceProvider => endpointCollectionProvider);
            };
            var webhostbuilder = new WebHostBuilder()
                .ConfigureServices(initialSvcConfig)
                .UseKestrel()
                .UseContentRoot(contentRoot);
            if (url != null)
            {
                webhostbuilder = webhostbuilder.UseUrls(url);
            }

            return webhostbuilder
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();
        }

        private static void SetUtf8OutputEncodingIfOutputIsRedirected()
        {
            if (IsOutputRedirected)
            {
                OutputEncoding = Encoding.UTF8;
            }
        }

        public static void Main(string[] args)
        {
            System.Net.ServicePointManager.ServerCertificateValidationCallback =
                ((sender, certificate, chain, sslPolicyErrors) => true);

            ParsedCommandLine parsedArguments;
            try
            {
                parsedArguments = CommandLineParser.ParseArguments(args);
            }
            catch (CommandLineParsingException clpe)
            {
                Error.WriteLine($"ERROR: {clpe.Message}");
                return;
            }            

            Debug.Assert(Directory.Exists(parsedArguments.EndpointCollectionDirectory));
            var endpointCollection = EndpointCollectionReader.ReadFromDirectory(parsedArguments.EndpointCollectionDirectory);

            if (parsedArguments.Command != CommandLineParser.COMMAND_SERVICE)
            {
                SetUtf8OutputEncodingIfOutputIsRedirected();
            }

            switch (parsedArguments.Command)
            {
                case CommandLineParser.COMMAND_NORMAL:
                    WriteLine("Admin interface available on /__netmockery");
                    Startup.TestMode = parsedArguments.TestMode;
                    CreateWebHost(parsedArguments.EndpointCollectionDirectory, Directory.GetCurrentDirectory(), parsedArguments.Url).Run();
                    break;

                case CommandLineParser.COMMAND_SERVICE:
                    RunAsService(parsedArguments);
                    break;

                case CommandLineParser.COMMAND_TEST:
                    Test(parsedArguments, endpointCollection);
                    break;

                case CommandLineParser.COMMAND_DUMP:
                    Dump(endpointCollection);
                    break;

              }
        }

        static public void RunAsService(ParsedCommandLine commandArgs)
        {
            CreateWebHost(commandArgs.EndpointCollectionDirectory, AppDomain.CurrentDomain.BaseDirectory, commandArgs.Url).RunAsService();
        }


        public static void Test(ParsedCommandLine commandArgs, EndpointCollection endpointCollection)
        {
            if (TestRunner.HasTestSuite(endpointCollection.SourceDirectory))
            {
                var testRunner = new ConsoleTestRunner(endpointCollection);
                if (commandArgs.Url != null)
                {
                    testRunner.Url = commandArgs.Url;
                }
                if (commandArgs.Only != null)
                {
                    var index = int.Parse(commandArgs.Only);
                    if (commandArgs.ShowResponse)
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
                    testRunner.TestAll(commandArgs.Stop, true);
                }                
            }
            else
            {
                Error.WriteLine("ERROR: No test suite found");
            }
        }


        public static void ViewScript(string[] commandArgs)
        {
            var scriptfile = commandArgs[0];
            Console.WriteLine(DynamicResponseCreatorBase.ExecuteIncludes(File.ReadAllText(scriptfile), Path.GetDirectoryName(scriptfile)));
        }


        public static void Dump(EndpointCollection endpointCollection)
        {
            foreach (var endpoint in endpointCollection.Endpoints)
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
