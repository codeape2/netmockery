using netmockery;
using Xunit;

using static netmockery.CommandLineParser;


namespace UnitTests
{
    public class TestCommandLineParser
    {
        [Fact]
        public void WebCommand()
        {
            var result = ParseArguments(new[] { "--command", "web", "--endpoints", "c:\\dir\\foo" });
            Assert.Equal(COMMAND_WEB, result.Command);
            Assert.Equal("c:\\dir\\foo", result.Endpoints);
            Assert.Null(result.Urls);
        }

        [Fact]
        public void WebCommandWithVaryingCasing()
        {
            var result = ParseArguments(new[] { "--COMMAND", "web", "--EndPoints", "c:\\dir\\foo" });
            Assert.Equal(COMMAND_WEB, result.Command);
            Assert.Equal("c:\\dir\\foo", result.Endpoints);
        }

        [Fact]
        public void WebCommandNoTestMode()
        {
            var result = ParseArguments(new[] { "--command", "web", "--endpoints", "c:\\dir\\foo", "--notestmode" });
            Assert.Equal(COMMAND_WEB, result.Command);
            Assert.Equal("c:\\dir\\foo", result.Endpoints);
            Assert.True(result.NoTestMode);

        }

        [Fact]
        public void WebCommandWithUrls()
        {
            var result = ParseArguments(new[] { "--command", "web", "--endpoints", "c:\\dir\\foo", "--urls", "http://*:5000" });
            Assert.Equal(COMMAND_WEB, result.Command);
            Assert.Equal("c:\\dir\\foo", result.Endpoints);
            Assert.Equal("http://*:5000", result.Urls);
        }

        [Fact]
        public void MissingCommand()
        {
            AssertGivesException("Missing required switch --command", new[] { "--endpoints", "c:\\foo\\bar" });
        }

        [Fact]
        public void UnknownCommand()
        {
            AssertGivesException("Unknown command 'foobar'", new[] { "--command", "foobar", "--endpoints", "c:\\foo\\bar" });
        }

        [Fact]
        public void MissingEndpoints()
        {
            AssertGivesException("Missing required switch --endpoints", new[] { "--command", "web" });
        }

        [Fact]
        public void TestCommand()
        {
            var result = ParseArguments(new[] { "--command", "test", "--endpoints", "c:\\dir\\foo" });
            Assert.Equal(COMMAND_TEST, result.Command);
            Assert.False(result.ShowResponse);
            Assert.Null(result.Only);
        }

        [Fact]
        public void TestCommandWithOptions()
        {
            var result = ParseArguments(new[] { "--command", "test", "--endpoints", "c:\\dir\\foo", "--only", "1", "--showresponse" });
            Assert.Equal(COMMAND_TEST, result.Command);
            Assert.Equal("1", result.Only);
            Assert.True(result.ShowResponse);
            Assert.False(result.Stop);
        }

        [Fact]
        public void TestWithStopOption()
        {
            var result = ParseArguments(new[] { "--command", "test", "--endpoints", "c:\\dir\\foo", "--stop" });
            Assert.Equal(COMMAND_TEST, result.Command);
            Assert.Null(result.Only);
            Assert.False(result.ShowResponse);
            Assert.True(result.Stop);
        }

        [Fact]
        public void DumpCommand()
        {
            var result = ParseArguments(new[] { "--command", "dump", "--endpoints", "c:\\foo\\bar" });
            Assert.Equal(COMMAND_DUMP, result.Command);
            Assert.Equal("c:\\foo\\bar", result.Endpoints);
        }

        [Fact]
        public void InvalidArgumentsForDumpCommand()
        {
            AssertGivesException("'--only' is not a valid argument for the 'dump' command", new[] { "--command", "dump", "--endpoints", "c:\\foo\\bar", "--only", "2" });
            AssertGivesException("'--urls' is not a valid argument for the 'dump' command", new[] { "--command", "dump", "--endpoints", "c:\\foo\\bar", "--urls", "http://localhost:5000/" });
        }

        [Fact]
        public void InvalidArgumentsForWebCommand()
        {
            AssertGivesException("'--only' is not a valid argument for the 'web' command", new[] { "--command", "web", "--endpoints", "c:\\foo\\bar", "--only", "2" });
        }

        private void AssertGivesException(string expectedMessage, string[] args)
        {
            var tx = Assert.Throws<CommandLineParsingException>(() => ParseArguments(args));
            Assert.Equal(expectedMessage, tx.Message);
        }
    }
}
