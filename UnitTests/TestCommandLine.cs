using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using netmockery;

namespace UnitTests
{
    public class TestCommandLine
    {
        [Fact]
        public void CreateWebHostCorrectly()
        {
            var webhost = Program.BuildWebApplication("examples/example1", new string[] {} );
            Assert.NotNull(webhost);
        }

        string[] NAMES = new[] { "Test one", "Test two", "Heisann" };

        [Fact]
        public void OnlySingleNumber()
        {
            Assert.Equal(new[] { 5 }, Program.ParseOnlyArgument("5", NAMES));
        }

        [Fact]
        public void OnlyListOfNumbers()
        {
            Assert.Equal(new[] { 5, 7, 11 }, Program.ParseOnlyArgument("5,7,11", NAMES));
        }

        [Fact]
        public void OnlyMatchString()
        {
            Assert.Equal(new[] { 0, 1 }, Program.ParseOnlyArgument("test", NAMES));
            Assert.Equal(new[] { 2 }, Program.ParseOnlyArgument("sann", NAMES));
        }

        [Fact]
        public void OnlyNoMatch()
        {
            Assert.Equal(0, Program.ParseOnlyArgument("foobar", NAMES).Length);
        }
    }
}
