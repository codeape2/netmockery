using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using netmockery;
using Xunit;

using static netmockery.CommandLineParser;



namespace UnitTests
{
    public class TestCommandLineParser
    {
        [Fact]
        public void ParsesWithNoCommand()
        {
            var result = ParseArguments(new[] { "c:\\dir\\foo" });

            Assert.Equal(COMMAND_NORMAL, result.Command);
            Assert.Equal("c:\\dir\\foo", result.EndpointCollectionDirectory);
            Assert.Null(result.Urls);
        }

        [Fact]
        public void NormalNoTestMode()
        {
            var result = ParseArguments(new[] { "c:\\dir\\foo", "--notestmode" });
            Assert.Equal(COMMAND_NORMAL, result.Command);
            Assert.Equal("c:\\dir\\foo", result.EndpointCollectionDirectory);
            Assert.True(result.NoTestMode);

        }

        [Fact]
        public void NormalExecutionCanHaveUrl()
        {
            var result = ParseArguments(new[] { "c:\\dir\\foo", "--url", "http://*:5000" });
            Assert.Equal(COMMAND_NORMAL, result.Command);
            Assert.Equal("c:\\dir\\foo", result.EndpointCollectionDirectory);
            Assert.Equal("http://*:5000", result.Urls);
        }

        [Fact]
        public void MissingEndpointDirectoryGivesError()
        {
            AssertGivesException("No endpoint directory specified", new string[0]);
        }

        [Fact]
        public void UnknownCommand()
        {
            AssertGivesException("Unknown command 'foobar'", new[] { "c:\\foo\\bar", "foobar" });
        }

        [Fact]
        public void TestCommand()
        {
            var result = ParseArguments(new[] { "c:\\dir\\foo", "test" });
            Assert.Equal(COMMAND_TEST, result.Command);
            Assert.False(result.ShowResponse);
            Assert.Null(result.Only);
        }

        [Fact]
        public void TestCommandWithOptions()
        {
            var result = ParseArguments(new[] { "c:\\dir\\foo", "test", "--only", "1", "--showResponse" });
            Assert.Equal(COMMAND_TEST, result.Command);
            Assert.Equal("1", result.Only);
            Assert.True(result.ShowResponse);
            Assert.False(result.Stop);
        }

        [Fact]
        public void TestWithStopOption()
        {
            var result = ParseArguments(new[] { "c:\\dir\\foo", "test", "--stop" });
            Assert.Equal(COMMAND_TEST, result.Command);
            Assert.Null(result.Only);
            Assert.False(result.ShowResponse);
            Assert.True(result.Stop);
        }

        [Fact]
        public void DumpCommand()
        {
            var result = ParseArguments(new[] { "c:\\foo\\bar", "dump" });
            Assert.Equal(COMMAND_DUMP, result.Command);
            Assert.Equal("c:\\foo\\bar", result.EndpointCollectionDirectory);
        }

        [Fact]
        public void InvalidArgumentsForDumpCommand()
        {
            AssertGivesException("'--only' is not a valid argument for the 'dump' command", new[] { "c:\\foo\\bar", "dump", "--only", "2" });
            AssertGivesException("'--url' is not a valid argument for the 'dump' command", new[] { "c:\\foo\\bar", "dump", "--url", "http://localhost:5000/" });
        }

        [Fact]
        public void InvalidArgumentsForNormalCommand()
        {
            AssertGivesException("'--only' is not a valid argument", new[] { "c:\\foo\\bar", "--only", "2" });
        }

        private void AssertGivesException(string expectedMessage, string[] args)
        {
            var tx = Assert.Throws<CommandLineParsingException>(() => ParseArguments(args));
            Assert.Equal(expectedMessage, tx.Message);
        }
    }
}
