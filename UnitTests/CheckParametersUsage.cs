using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using netmockery;
using Xunit;
using System.IO;

namespace UnitTests
{
    public class CheckParametersUsage : IDisposable
    {
        private Endpoint endpoint;
        private EndpointParameter filenameParam;
        private EndpointParameter contenttypeParam;
        private EndpointParameter fooParam;
        private EndpointParameter delayParam;
        private EndpointParameter statusCodeParam;
        private DirectoryCreator dc = new DirectoryCreator();

        public CheckParametersUsage()
        {
            endpoint = new Endpoint("foo", "bar")
            {
                Directory = dc.DirectoryName
            };
            filenameParam = new EndpointParameter
            {
                Name = "filename",
                DefaultValue = "file.txt"
            };
            contenttypeParam = new EndpointParameter
            {
                Name = "contenttype",
                DefaultValue = "text/plain"
            };
            fooParam = new EndpointParameter
            {
                Name = "foo",
                DefaultValue = "FOO"
            };
            delayParam = new EndpointParameter
            {
                Name = "delay",
                DefaultValue = "0"
            };
            statusCodeParam = new EndpointParameter
            {
                Name = "statuscode",
                DefaultValue = "200"
            };
            endpoint.AddParameter(filenameParam);
            endpoint.AddParameter(contenttypeParam);
            endpoint.AddParameter(fooParam);
            endpoint.AddParameter(delayParam);
            endpoint.AddParameter(statusCodeParam);
        }

        public void Dispose()
        {
            dc.Dispose();
        }

        [Fact]
        public void ParamNamesMustBeUnique()
        {
            Assert.Throws<ArgumentException>(() => { endpoint.AddParameter(filenameParam); });
        }

        [Fact]
        public void LookupOfMissingParameterGivesError()
        {
            var ae = Assert.Throws<ArgumentException>(() => { endpoint.GetParameter("foobar"); });
            Assert.Equal("Endpoint parameter 'foobar' not found", ae.Message);
        }

        [Fact]
        public void GetParameterByWrongIndexGivesError()
        {
            var ae = Assert.Throws<ArgumentException>(() => { endpoint.GetParameter(-1); });
            Assert.Equal("Invalid parameter index -1", ae.Message);

            ae = Assert.Throws<ArgumentException>(() => { endpoint.GetParameter(endpoint.ParameterCount); });
            Assert.Equal("Invalid parameter index 5", ae.Message);
        }

        [Fact]
        public void LookupsWorkAsExpected()
        {
            Assert.Equal("filename", endpoint.GetParameter("filename").Name);
            Assert.Equal("filename", endpoint.GetParameter(0).Name);

            Assert.Equal("statuscode", endpoint.GetParameter(endpoint.ParameterCount - 1).Name);
        }

        [Fact]
        public async Task CanUseParamForFilename()
        {
            dc.AddFile("file.txt", "CONTENTS0");
            dc.AddFile("otherfile.txt", "CONTENTS1");

            var responseCreator = new FileResponse("$filename", endpoint);
            Assert.Equal(Path.Combine(endpoint.Directory, "file.txt"), responseCreator.Filename);
            Assert.Equal("CONTENTS0", (await GetResponseAsync(responseCreator)).WrittenContent);

            filenameParam.Value = "otherfile.txt";
            Assert.Equal(Path.Combine(endpoint.Directory, "otherfile.txt"), responseCreator.Filename);
            Assert.Equal("CONTENTS1", (await GetResponseAsync(responseCreator)).WrittenContent);
        }

        [Fact]
        public async Task CanUseParamForContenttype()
        {
            dc.AddFile("file.txt", "Heisann");
            var responseCreator = new FileResponse("file.txt", endpoint)
            {
                ContentType = "$contenttype"
            };
            Assert.Equal("text/plain", responseCreator.ContentType);
            Assert.Equal("text/plain; charset=utf-8", (await GetResponseAsync(responseCreator)).ContentType);

            contenttypeParam.Value = "application/xml";
            Assert.Equal("application/xml; charset=utf-8", (await GetResponseAsync(responseCreator)).ContentType);
        }

        [Fact]
        public async Task CanUseParamForLiteral()
        {
            var responseCreator = new LiteralResponse("$foo", endpoint);
            Assert.Equal("FOO", await responseCreator.GetBodyAsync(null));

            fooParam.Value = "BAR";
            Assert.Equal("BAR", await responseCreator.GetBodyAsync(null));
            Assert.Equal("BAR", (await GetResponseAsync(responseCreator)).WrittenContent);
        }


        async Task<TestableHttpResponse> GetResponseAsync(ResponseCreator responseCreator) 
        {
            var request = new TestableHttpRequest(null, null);
            var retval = new TestableHttpResponse();
            await responseCreator.CreateResponseAsync(request, new byte[0], retval, endpoint);
            return retval;
        }

        [Fact]
        public void CanUseParamForDelay()
        {
            var responseCreator = new FileDynamicResponseCreator("file.txt", endpoint);
            responseCreator.SetDelayFromConfigValue("$delay");

            Assert.Equal(0, responseCreator.Delay);
            delayParam.Value = "1000";
            Assert.Equal(1000, responseCreator.Delay);
        }
    }
}
