using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using netmockery;

namespace UnitTests
{
    public class TestResponseReplacements
    {
        [Fact]
        public void ReplacementsAreExecuted()
        {
            var responseCreator = new LiteralResponse("abc def");
            responseCreator.Replacements = new[]
            {
                new BodyReplacement { SearchTerm = "abc", ReplacementTerm = "ABC" },
                new BodyReplacement { SearchTerm = "def", ReplacementTerm = "DEF" }
            };

            var body = responseCreator.GetBodyAndExecuteReplacements(null);
            Assert.Equal("ABC DEF", body);
        }
    }
}
