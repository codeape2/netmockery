using netmockery;
using System;
using Xunit;

using static netmockery.CommandLineParser;


namespace UnitTests
{
    public class TestCommandLineParser
    {
        [Fact]
        public void WebCommandAsDefault()
        {
            var result = ParseArguments(new[] { "--endpoints", "c:\\dir\\foo" });
            Assert.Equal(COMMAND_WEB, result.Command);
        }

        [Fact]
        public void WebCommand()
        {
            var result = ParseArguments(new[] { "web", "--endpoints", "c:\\dir\\foo" });
            Assert.Equal(COMMAND_WEB, result.Command);
            Assert.Equal("c:\\dir\\foo", result.Endpoints);
            Assert.Null(result.Urls);
        }

        [Fact]
        public void WebCommandWithVaryingArgCasing()
        {
            var result = ParseArguments(new[] { "web", "--EndPoints", "c:\\dir\\foo" });
            Assert.Equal(COMMAND_WEB, result.Command);
            Assert.Equal("c:\\dir\\foo", result.Endpoints);
        }

        [Fact]
        public void WebCommandNoTestMode()
        {
            var result = ParseArguments(new[] { "web", "--endpoints", "c:\\dir\\foo", "--notestmode" });
            Assert.Equal(COMMAND_WEB, result.Command);
            Assert.Equal("c:\\dir\\foo", result.Endpoints);
            Assert.True(result.NoTestMode);

        }

        [Fact]
        public void WebCommandWithUrls()
        {
            var result = ParseArguments(new[] { "web", "--endpoints", "c:\\dir\\foo", "--urls", "http://*:5000" });
            Assert.Equal(COMMAND_WEB, result.Command);
            Assert.Equal("c:\\dir\\foo", result.Endpoints);
            Assert.Equal("http://*:5000", result.Urls);
        }

        [Fact]
        public void UnknownCommand()
        {
            AssertGivesException("Unknown command 'webz'", new[] { "webz", "--endpoints", "c:\\foo\\bar" });
        }

        [Fact]
        public void MissingEndpoints()
        {
            AssertGivesException("Missing required switch --endpoints", new[] { "web" });
        }

        [Fact]
        public void TestCommand()
        {
            var result = ParseArguments(new[] { "test", "--endpoints", "c:\\dir\\foo" });
            Assert.Equal(COMMAND_TEST, result.Command);
            Assert.False(result.ShowResponse);
            Assert.Null(result.Only);
        }

        [Fact]
        public void TestCommandWhenUpperCase()
        {
            var result = ParseArguments(new[] { "TEST", "--endpoints", "c:\\dir\\foo" });
            Assert.Equal(COMMAND_TEST, result.Command);
        }

        [Fact]
        public void TestCommandWithOptions()
        {
            var result = ParseArguments(new[] { "test", "--endpoints", "c:\\dir\\foo", "--only", "1", "--showresponse" });
            Assert.Equal(COMMAND_TEST, result.Command);
            Assert.Equal("1", result.Only);
            Assert.True(result.ShowResponse);
            Assert.False(result.Stop);
        }

        [Fact]
        public void TestWithStopOption()
        {
            var result = ParseArguments(new[] { "test", "--endpoints", "c:\\dir\\foo", "--stop" });
            Assert.Equal(COMMAND_TEST, result.Command);
            Assert.Null(result.Only);
            Assert.False(result.ShowResponse);
            Assert.True(result.Stop);
        }

        [Fact]
        public void DumpCommand()
        {
            var result = ParseArguments(new[] { "dump", "--endpoints", "c:\\foo\\bar" });
            Assert.Equal(COMMAND_DUMP, result.Command);
            Assert.Equal("c:\\foo\\bar", result.Endpoints);
        }

        [Fact]
        public void InvalidArgumentsForDumpCommand()
        {
            AssertGivesException("'--only' is not a valid argument for the 'dump' command", new[] { "dump", "--endpoints", "c:\\foo\\bar", "--only", "2" });
            AssertGivesException("'--urls' is not a valid argument for the 'dump' command", new[] { "dump", "--endpoints", "c:\\foo\\bar", "--urls", "http://localhost:5000/" });
        }

        [Fact]
        public void InvalidArgumentsForWebCommand()
        {
            AssertGivesException("'--only' is not a valid argument for the 'web' command", new[] { "web", "--endpoints", "c:\\foo\\bar", "--only", "2" });
        }

        [Fact]
        public void NoArguments()
        {
            AssertGivesException("No arguments", Array.Empty<string>());
        }

        private void AssertGivesException(string expectedMessage, string[] args)
        {
            var ex = Assert.Throws<CommandLineParsingException>(() => ParseArguments(args));
            Assert.Equal(expectedMessage, ex.Message);
        }
    }
}
