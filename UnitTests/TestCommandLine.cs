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
            var webhost = Program.CreateWebHost("examples\\example1", ".", null);
            Assert.NotNull(webhost);
        }
    }
}
