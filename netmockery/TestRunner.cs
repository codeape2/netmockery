using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static System.Console;

namespace netmockery
{
    public class TestRunner
    {
        private IEnumerable<NetmockeryTestCase> testcases;
        private EndpointCollection endpointCollection;

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


        public void TestAll()
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
            }
            WriteLine();
            WriteLine($"Total: {testcases.Count()} Errors: {errors}");
        }

        public NetmockeryTestCaseResult ExecuteTestAndOutputResult(int index)
        {
            return ExecuteTestAndOutputResult(index, testcases.ElementAt(index));
        }

        public NetmockeryTestCaseResult ExecuteTestAndOutputResult(int index, NetmockeryTestCase test)
        {
            Write($"{index.ToString().PadLeft(3)} {test.Name.PadRight(60)}");
            var result = test.ExecuteAsync(endpointCollection).Result;
            WriteLine(result.ResultAsString);
            return result;
        }

        public void ShowResponse(int index)
        {
            var testCase = testcases.ElementAt(index);
            var response = testCase.GetResponseAsync(endpointCollection).Result;
            if (response.Item2 != null)
            {
                Error.WriteLine($"ERROR: {response.Item2}");
            }
            else
            {
                if (IsOutputRedirected)
                {
                    Write(response.Item1);
                }
                else
                {
                    WriteLine(response.Item1);
                }                
            }
        }

    }
}
