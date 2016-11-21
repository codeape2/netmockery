using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using static System.Console;

namespace netmockery
{
    public class TestRunner
    {
        private EndpointTestDefinition endpointTestDefinition;
        private EndpointCollection endpointCollection;

        public TestRunner(EndpointCollection endpointCollection)
        {
            this.endpointCollection = endpointCollection;
            Debug.Assert(EndpointTestDefinition.HasTestSuite(endpointCollection.SourceDirectory));
            endpointTestDefinition = EndpointTestDefinition.ReadFromDirectory(endpointCollection.SourceDirectory);

            endpointTestDefinition.SetStaticTimeIfConfigured(endpointCollection.SourceDirectory);
        }

        public void TestAll()
        {
            var errors = 0;
            var index = 0;
            foreach (var test in endpointTestDefinition.Tests)
            {
                var result = ExecuteTestAndOutputResult(index++, test);
                if (result.Error)
                {
                    errors++;
                }
            }
            WriteLine();
            WriteLine($"Total: {endpointTestDefinition.Tests.Count()} Errors: {errors}");
        }

        public NetmockeryTestCaseResult ExecuteTestAndOutputResult(int index)
        {
            return ExecuteTestAndOutputResult(index, endpointTestDefinition.Tests.ElementAt(index));
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
            var testCase = endpointTestDefinition.Tests.ElementAt(index);
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
