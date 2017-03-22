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
            endpoint = new Endpoint("foo", "bar");
            endpoint.Directory = dc.DirectoryName;
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
        public void CanUseParamForFilename()
        {
            dc.AddFile("file.txt", "CONTENTS0");
            dc.AddFile("otherfile.txt", "CONTENTS1");

            var responseCreator = new FileResponse("$filename", endpoint);
            Assert.Equal(Path.Combine(endpoint.Directory, "file.txt"), responseCreator.Filename);
            Assert.Equal("CONTENTS0", GetResponse(responseCreator).WrittenContent);

            filenameParam.Value = "otherfile.txt";
            Assert.Equal(Path.Combine(endpoint.Directory, "otherfile.txt"), responseCreator.Filename);
            Assert.Equal("CONTENTS1", GetResponse(responseCreator).WrittenContent);
        }

        [Fact]
        public void CanUseParamForContenttype()
        {
            dc.AddFile("file.txt", "Heisann");
            var responseCreator = new FileResponse("file.txt", endpoint);
            responseCreator.ContentType = "$contenttype";

            Assert.Equal("text/plain", responseCreator.ContentType);
            Assert.Equal("text/plain; charset=utf-8", GetResponse(responseCreator).ContentType);

            contenttypeParam.Value = "application/xml";
            Assert.Equal("application/xml; charset=utf-8", GetResponse(responseCreator).ContentType);
        }

        [Fact]
        public void CanUseParamForLiteral()
        {
            var responseCreator = new LiteralResponse("$foo", endpoint);
            Assert.Equal("FOO", responseCreator.GetBody(null));

            fooParam.Value = "BAR";
            Assert.Equal("BAR", responseCreator.GetBody(null));
            Assert.Equal("BAR", GetResponse(responseCreator).WrittenContent);
        }

        TestableHttpResponse GetResponse(ResponseCreator responseCreator)
        {
            var request = new TestableHttpRequest(null, null);
            var retval = new TestableHttpResponse();
            var bytesWritten = responseCreator.CreateResponseAsync(request, new byte[0], retval, endpoint).Result;
            return retval;
        }

        [Fact]
        public void CanUseParamForScriptFilename()
        {
            dc.AddFile("file.txt", "return \"I am file.txt: \" + GetParam(\"filename\");");
            dc.AddFile("another.txt", "return \"I am another.txt: \" + GetParam(\"filename\");");

            var responseCreator = new FileDynamicResponseCreator("$filename", endpoint);
            Assert.Equal(Path.Combine(endpoint.Directory, "file.txt"), responseCreator.Filename);
            Assert.Equal("I am file.txt: file.txt", GetResponse(responseCreator).WrittenContent);

            filenameParam.Value = "another.txt";
            Assert.Equal(Path.Combine(endpoint.Directory, "another.txt"), responseCreator.Filename);
            Assert.Equal("I am another.txt: another.txt", GetResponse(responseCreator).WrittenContent);
        }

        [Fact]
        public void CanUseParamForDelay()
        {
            var responseCreator = new FileDynamicResponseCreator("file.txt", endpoint);
            responseCreator.SetDelayFromString("$delay");

            Assert.Equal(0, responseCreator.Delay);
            delayParam.Value = "1000";
            Assert.Equal(1000, responseCreator.Delay);
        }
    }
}
