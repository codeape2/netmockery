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
        public async Task KnownConfigurationsTestOK()
        {
            var configurationsToTest = new List<string>();
            configurationsToTest.Add("examples/example1");

            configurationsToTest.Add("examples/withparams");
            configurationsToTest.Add("examples/documentation");

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
                await CheckConfigdirectoryAsync(directory);
            }
        }

        [Fact]
        public async Task ShowResponseWorksAsExpected()
        {
            await CheckOutputAsync("examples/example1", 0);
        }
        
        public async Task CheckConfigdirectoryAsync(string directory)
        {
            Assert.True(Directory.Exists(directory), $"Directory {directory} does not exist");
            
            var endpointCollection = EndpointCollectionReader.ReadFromDirectory(directory);
            Assert.True(endpointCollection.Endpoints.Count() > 0, $"No endpoints defined in {directory}");

            output.WriteLine(directory);

            if (! TestRunner.HasTestSuite(directory))
            {
                output.WriteLine($"No tests in {directory}");
                return;
            }

            var tests = new ConsoleTestRunner(endpointCollection);
            foreach (var test in tests.Tests)
            {
                output.WriteLine(test.Name);
                var result = await test.ExecuteAsync(endpointCollection, now: tests.Now);
                output.WriteLine(result.ResultAsString);
                Assert.True(result.OK, $"Test case {result.TestCase.Name}, message '{result.Message}'");
            }
        }

        public async Task CheckOutputAsync(string directory, int index)
        {
            var testRunner = new ConsoleTestRunner(EndpointCollectionReader.ReadFromDirectory(directory));
            await testRunner.ShowResponseAsync(index);
        }
    }
}
