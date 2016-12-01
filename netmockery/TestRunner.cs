using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;


namespace netmockery
{
    public abstract class TestRunner
    {
        private IEnumerable<NetmockeryTestCase> testcases;
        private EndpointCollection endpointCollection;

        public string Url { get; set; }

        public TestRunner(EndpointCollection endpointCollection)
        {
            this.endpointCollection = endpointCollection;
            Debug.Assert(HasTestSuite(endpointCollection.SourceDirectory));
            testcases = ReadFromDirectory(endpointCollection.SourceDirectory);
            SetStaticTimeIfConfigured(endpointCollection.SourceDirectory);
        }

        public IEnumerable<NetmockeryTestCase> Tests => testcases;

        public void SetStaticTimeIfConfigured(string directory)
        {
            if (File.Exists(now_txt_filename(directory)))
            {
                var contents = File.ReadAllText(now_txt_filename(directory));
                var datetime = DateTime.ParseExact(contents, "yyyy-MM-dd HH:mm:ss", null);
                RequestInfo.SetStaticNow(datetime);
            }
        }


        static public bool HasTestSuite(string directory) => File.Exists(tests_json_filename(directory));

        static private string tests_json_filename(string directory) => Path.Combine(tests_directory(directory), "tests.json");
        static private string now_txt_filename(string directory) => Path.Combine(tests_directory(directory), "now.txt");
        static private string tests_directory(string directory) => Path.Combine(directory, "tests");


        public static IEnumerable<NetmockeryTestCase> ReadFromDirectory(string directory)
        {
            var jsonTests = JsonConvert.DeserializeObject<List<JSONTest>>(File.ReadAllText(tests_json_filename(directory)));
            return from jsontest in jsonTests select jsontest.Validated().CreateTestCase(tests_directory(directory));
        }

        
        public void TestAll(bool stopAfterFirstFailure)
        {
            var errors = 0;
            var index = 0;
            foreach (var test in testcases)
            {
                var result = ExecuteTestAndOutputResult(index++, test);
                if (result.Error)
                {
                    errors++;
                }

                if (result.Error && stopAfterFirstFailure)
                {
                    break;
                }
            }
            WriteSummary(errors);
        }

        public NetmockeryTestCaseResult ExecuteTestAndOutputResult(int index)
        {
            return ExecuteTestAndOutputResult(index, testcases.ElementAt(index));
        }

        public NetmockeryTestCaseResult ExecuteTestAndOutputResult(int index, NetmockeryTestCase test)
        {
            WriteBeginTest(index, test);
            
            var result = Url != null ? test.ExecuteAgainstUrlAsync(Url).Result : test.ExecuteAsync(endpointCollection).Result;
            WriteResult(result);

            return result;
        }

        public void ShowResponse(int index)
        {
            var testCase = testcases.ElementAt(index);
            var response = testCase.GetResponseAsync(endpointCollection).Result;
            if (response.Item2 != null)
            {
                WriteError(response.Item2);
            }
            else
            {
                WriteResponse(response.Item1);
            }
        }

        public abstract void WriteBeginTest(int index, NetmockeryTestCase testcase);
        public abstract void WriteResult(NetmockeryTestCaseResult result);
        public abstract void WriteResponse(string response);
        public abstract void WriteSummary(int errors);
        public abstract void WriteError(string s);
    }

    public class ConsoleTestRunner : TestRunner
    {
        public ConsoleTestRunner(EndpointCollection endpointCollection) : base(endpointCollection)
        {
        }

        public override void WriteBeginTest(int index, NetmockeryTestCase testcase)
        {
            Console.Write($"{index.ToString().PadLeft(3)} {testcase.Name.PadRight(60)}");
        }

        public override void WriteError(string s)
        {
            Console.Error.WriteLine($"ERROR: {s}");
        }

        public override void WriteResponse(string response)
        {
            if (Console.IsOutputRedirected)
            {
                Console.Write(response);
            }
            else
            {
                Console.WriteLine(response);
            }

        }

        public override void WriteResult(NetmockeryTestCaseResult result)
        {
            Console.WriteLine(result.ResultAsString);
        }

        public override void WriteSummary(int errors)
        {
            Console.WriteLine();
            Console.WriteLine($"Total: {Tests.Count()} Errors: {errors}");
        }
    }
}
