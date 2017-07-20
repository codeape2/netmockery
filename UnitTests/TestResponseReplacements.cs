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
        public async Task ReplacementsAreExecuted()
        {
            var responseCreator = new LiteralResponse("abc def", new Endpoint("foo", "bar"))
            {
                Replacements = new[]
                {
                    new BodyReplacement { SearchTerm = "abc", ReplacementTerm = "ABC" },
                    new BodyReplacement { SearchTerm = "def", ReplacementTerm = "DEF" }
                }
            };
            var body = await responseCreator.GetBodyAndExecuteReplacementsAsync(null);
            Assert.Equal("ABC DEF", body);
        }
    }
}
