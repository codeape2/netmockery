using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace netmockery
{
    public class Program
    {
        public static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
        }

        public static async Task MainAsync(string[] args)
        {
            ParsedCommandLine parsedArguments;
            try
            {
                parsedArguments = CommandLineParser.ParseArguments(args);
            }
            catch (CommandLineParsingException clpe)
            {
                Console.Error.WriteLine($"ERROR: {clpe.Message}");
                return;
            }            

            if (!Directory.Exists(parsedArguments.Endpoints))
            {
                Console.Error.WriteLine("Directory not found");
                return;
            }
            
            var endpointCollection = EndpointCollectionReader.ReadFromDirectory(parsedArguments.Endpoints);

            switch (parsedArguments.Command)
            {
                case CommandLineParser.COMMAND_WEB:
                    if (endpointCollection.Endpoints.Count() == 0)
                    {
                        Console.WriteLine("No endpoints found");
                    }
                    Console.WriteLine("Admin interface available on /__netmockery");
                    Startup.TestMode = parsedArguments.TestMode;
                    BuildWebApplication(parsedArguments.Endpoints, args).Run();
                    break;

                case CommandLineParser.COMMAND_TEST:
                    await TestAsync(parsedArguments, endpointCollection);
                    break;

                case CommandLineParser.COMMAND_DUMP:
                    Dump(endpointCollection);
                    break;

                case CommandLineParser.COMMAND_DUMPREFS:
                    foreach (var metadataReference in DynamicResponseCreatorBase.GetDefaultMetadataReferences())
                    {
                        Console.WriteLine(metadataReference.Display);
                    }
                    break;

              }
        }

        public static WebApplication BuildWebApplication(string endpointCollectionDirectory, string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddMvc(options => options.EnableEndpointRouting = false);
            builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            builder.Services.AddSingleton(serviceProvider => new ResponseRegistry());
            builder.Services.AddSingleton(serviceProvider => new EndpointCollectionProvider(endpointCollectionDirectory));
            builder.Services.AddSingleton(serviceProvider => serviceProvider.GetService<EndpointCollectionProvider>().EndpointCollection);
            var app = builder.Build();

            new Startup().Configure(app);

            return app;
        }

        public static async Task TestAsync(ParsedCommandLine commandArgs, EndpointCollection endpointCollection)
        {
            if (!TestRunner.HasTestSuite(endpointCollection.SourceDirectory))
            {
                Console.Error.WriteLine("ERROR: No test suite found");
                return;
            }

            if (commandArgs.Diff && commandArgs.Only == null)
            {
                Console.Error.WriteLine("ERROR: --diff can only be specified with --only");
                return;
            }

            var testRunner = new ConsoleTestRunner(endpointCollection);

            if (commandArgs.Only != null)
            {
                var indexes = ParseOnlyArgument(commandArgs.Only, (from testCase in testRunner.Tests select testCase.Name).ToArray());
                if (indexes.Length == 0)
                {
                    Console.Error.WriteLine("ERROR: No testcases matches --only");
                }

                foreach (var index in indexes)
                {
                    if (commandArgs.Diff)
                    {
                        var diffTool = Environment.GetEnvironmentVariable("DIFFTOOL");
                        if (diffTool == null)
                        {
                            Console.Error.WriteLine("ERROR: No diff tool configured. Set DIFFTOOL environment variable to point to executable.");
                            return;
                        }

                        var testCase = testRunner.Tests.ElementAt(index);
                        if (testCase.ExpectedResponseBody == null)
                        {
                            Console.Error.WriteLine($"ERROR: Test case has no expected response body");
                            return;
                        }

                        var responseTuple = await testCase.GetResponseAsync(endpointCollection, testRunner.Now);
                        if (responseTuple.Item2 != null)
                        {
                            Console.Error.WriteLine($"ERROR: {responseTuple.Item2}");
                            return;
                        }

                        var expectedFilename = Path.GetTempFileName();
                        var actualFilename = Path.GetTempFileName();

                        File.WriteAllText(expectedFilename, testCase.ExpectedResponseBody);
                        File.WriteAllText(actualFilename, responseTuple.Item1);

                        StartExternalDiffTool(diffTool, expectedFilename, actualFilename);
                    }
                    else
                    {
                        if (commandArgs.ShowResponse)
                        {
                            await testRunner.ShowResponseAsync(index);
                        }
                        else
                        {
                            await testRunner.ExecuteTestAndOutputResultAsync(index);
                        }
                    }
                }
            }
            else if (commandArgs.List)
            {
                testRunner.ListAllTestNames();
            }
            else
            {
                await testRunner.TestAllAsync(commandArgs.Stop, true);
            }                
        }

        public static int[] ParseOnlyArgument(string only, string[] names)
        {
            if (Regex.IsMatch(only, @"^\d+$"))
            {
                return new[] { int.Parse(only) };
            }
            else if (Regex.IsMatch(only, @"^(\d+)(,\d+)+$"))
            {
                return (from strval in only.Split(',') select int.Parse(strval)).ToArray();
            }
            else
            {
                return (from i in Enumerable.Range(0, names.Length) where names[i].ToLower().Contains(only.ToLower()) select i).ToArray();
            }
        }

        public static void StartExternalDiffTool(string diffTool, string expectedFilename, string actualFilename)
        {
            Debug.Assert(diffTool != null);
            Debug.Assert(File.Exists(expectedFilename));
            Debug.Assert(File.Exists(actualFilename));
            Console.WriteLine($"Starting external diff tool {diffTool}");
            Process.Start(diffTool, $"\"{expectedFilename}\" \"{actualFilename}\"");
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
                Console.WriteLine($"{endpoint.Name} {endpoint.PathRegex}");
                foreach (var response in endpoint.Responses)
                {
                    Console.WriteLine($"    {response.Item1} -> {response.Item2}");
                }
            }
        }

        public static string NetmockeryVersion
        {
            get
            {
                var version = typeof(netmockery.Program).GetTypeInfo().Assembly.GetName().Version;
                return $"{version.Major}.{version.Minor}.{version.MinorRevision}";
            }
        }
    }
}
