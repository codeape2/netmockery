using netmockery;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace UnitTests
{
    public class IntegrationTests
    {
        private ITestOutputHelper output;

        public IntegrationTests(ITestOutputHelper output)
        {
            this.output = output;
        }
        private const string FILENAME = "configurations_to_test.txt";

        [Fact]
        public void KnownConfigurationsTestOK()
        {
            var configurationsToTest = new List<string>();
            configurationsToTest.Add("examples/example1");
            if (File.Exists(FILENAME))
            {
                configurationsToTest.AddRange(from line in File.ReadAllLines(FILENAME) where !string.IsNullOrEmpty(line) && !line.StartsWith("#") select line);
            }

            output.WriteLine("Configurations:");
            foreach (var directory in configurationsToTest)
            {
                output.WriteLine($"    {directory}");
            }
            output.WriteLine("");

            foreach (var directory in configurationsToTest)
            {
                CheckConfigdirectory(directory);
            }
        }

        [Fact]
        public void ShowResponseWorksAsExpected()
        {
            CheckOutput("examples/example1", 0);
        }
        
        public void CheckConfigdirectory(string directory)
        {
            Assert.True(Directory.Exists(directory), $"Directory {directory} does not exist");
            Assert.True(TestRunner.HasTestSuite(directory), $"Directory {directory} has not test suite");
            var endpointCollection = EndpointCollectionReader.ReadFromDirectory(directory);
            Assert.True(endpointCollection.Endpoints.Count() > 0, $"No endpoints defined in {directory}");

            output.WriteLine(directory);
            var tests = new ConsoleTestRunner(endpointCollection);
            foreach (var test in tests.Tests)
            {
                output.WriteLine(test.Name);
                var result = test.ExecuteAsync(endpointCollection, now: tests.Now).Result;
                output.WriteLine(result.ResultAsString);
                Assert.True(result.OK, $"Test case {result.TestCase.Name}, message '{result.Message}'");
            }
        }

        public void CheckOutput(string directory, int index)
        {
            var testRunner = new ConsoleTestRunner(EndpointCollectionReader.ReadFromDirectory(directory));
            testRunner.ShowResponse(index);
        }
    }
}
