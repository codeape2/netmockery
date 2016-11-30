﻿using System;
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
            Assert.Equal(null, result.Url);
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
        public void SwitchesCanComeBeforePositionalArguments()
        {
            var result = ParseArguments(new[] { "--url", "http://*:5000", "c:\\dir\\foo", "service" });
            Assert.Equal(COMMAND_SERVICE, result.Command);
            Assert.Equal("c:\\dir\\foo", result.EndpointCollectionDirectory);
            Assert.Equal("http://*:5000", result.Url);
        }

        [Fact]
        public void SwitchesCanComeBetweenDirectoryAndCommand()
        {
            var result = ParseArguments(new[] { "c:\\dir\\foo", "--url", "http://*:5000", "service" });
            Assert.Equal(COMMAND_SERVICE, result.Command);
            Assert.Equal("c:\\dir\\foo", result.EndpointCollectionDirectory);
            Assert.Equal("http://*:5000", result.Url);
        }


        [Fact]
        public void NormalExecutionCanHaveUrl()
        {
            var result = ParseArguments(new[] { "c:\\dir\\foo", "--url", "http://*:5000" });
            Assert.Equal(COMMAND_NORMAL, result.Command);
            Assert.Equal("c:\\dir\\foo", result.EndpointCollectionDirectory);
            Assert.Equal("http://*:5000", result.Url);
        }

        [Fact]
        public void RunAsService()
        {
            var result = ParseArguments(new[] { "c:\\dir\\foo", "service", "--url", "http://*:5000" });
            Assert.Equal(COMMAND_SERVICE, result.Command);
            Assert.Equal("c:\\dir\\foo", result.EndpointCollectionDirectory);
            Assert.Equal("http://*:5000", result.Url);
        }

        [Fact]
        public void RunAsServiceWithoutUrl()
        {
            var result = ParseArguments(new[] { "c:\\dir\\foo", "service" });
            Assert.Equal(COMMAND_SERVICE, result.Command);
            Assert.Equal("c:\\dir\\foo", result.EndpointCollectionDirectory);
            Assert.Equal(null, result.Url);
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
        }

        [Fact]
        public void OnlyOptionMustBeNumeric()
        {
            AssertGivesException("Argument --only: integer required", new[] { "c:\\dir\\foo", "test", "--only", "a" });
        }

        [Fact]
        public void DumpCommand()
        {
            var result = ParseArguments(new[] { "c:\\foo\\bar", "dump" });
            Assert.Equal(COMMAND_DUMP, result.Command);
            Assert.Equal("c:\\foo\\bar", result.EndpointCollectionDirectory);
        }

        public void AssertGivesException(string expectedMessage, string[] args)
        {
            var tx = Assert.Throws<CommandLineParsingException>(() => ParseArguments(args));
            Assert.Equal(expectedMessage, tx.Message);
        }
    }
}
