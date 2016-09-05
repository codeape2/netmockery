using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using netmockery;

namespace UnitTests
{
    public class TestXPathMatcher
    {
        public const string BODY = @"
<s:Envelope xmlns:s=""http://schemas.xmlsoap.org/soap/envelope/"">
	<s:Header>
		<ActivityId CorrelationId=""4e0f2920-5cdc-4e1a-8083-dce285c64b8b"" xmlns=""http://schemas.microsoft.com/2004/09/ServiceModel/Diagnostics"">d68a0940-01b5-4153-8d19-4282fbdf6d02</ActivityId>
	</s:Header>
	<s:Body>
		<GetGPCommunicationDetails xmlns=""http://register.nhn.no/Orchestration"">
			<ssn>13116900216</ssn>
		</GetGPCommunicationDetails>
	</s:Body>
</s:Envelope>
";

        [Fact]
        public void CanMatchBody()
        {
            var matcher = new XPathMatcher("//nhn:ssn = '13116900216'");
            matcher.AddNamespace("nhn", "http://register.nhn.no/Orchestration");
            Assert.True(matcher.Matches(null, BODY, null));
        }

        [Fact]
        public void NoMatch()
        {
            var matcher = new XPathMatcher("//nhn:ssn = '13116900217'");
            matcher.AddNamespace("nhn", "http://register.nhn.no/Orchestration");
            Assert.False(matcher.Matches(null, BODY, null));
        }
    }
}
