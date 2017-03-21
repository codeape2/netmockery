using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using netmockery;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Net;
using System.Text;

namespace UnitTests
{
    public class TestEndpointParam
    {
        [Fact]
        public void ValueAndDefaultWorksAsExpected()
        {
            var ep = new EndpointParameter { DefaultValue = "a" };
            Assert.Equal("a", ep.Value);
            Assert.True(ep.ValueIsDefault);

            ep.Value = "b";
            Assert.Equal("b", ep.Value);
            Assert.False(ep.ValueIsDefault);
        }
    }
    public class FileResponseCreatorCanUseParamForFilename : IDisposable
    {
        private Endpoint endpoint;
        private EndpointParameter filenameParam;
        private EndpointParameter contenttypeParam;
        private EndpointParameter fooParam;
        private EndpointParameter delayParam;
        private EndpointParameter statusCodeParam;
        private DirectoryCreator dc = new DirectoryCreator();

        public FileResponseCreatorCanUseParamForFilename()
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
    public class TestEndpointWithParams : IDisposable
    {
        private DirectoryCreator dc = new DirectoryCreator();

        private EndpointCollection CreateEndpointWithScript()
        {
            dc.AddFile("endpoint\\endpoint.json", JsonConvert.SerializeObject(DataUtils.CreateScriptEndpoint("endpoint", "script.csscript")));
            dc.AddFile("endpoint\\script.csscript", "return GetParam(\"greeting\") + \" \" + GetParam(\"subject\");");
            dc.AddFile("endpoint\\params.json", JsonConvert.SerializeObject(new[] {
                new JSONParam { name = "greeting", @default = "Hello", description = "foo" },
                new JSONParam { name = "subject", @default = "World", description = "foo" },
            }));

            return EndpointCollectionReader.ReadFromDirectory(dc.DirectoryName);
        }

        [Fact]
        public void CanReadParams()
        {
            var ecr = CreateEndpointWithScript();
            Assert.Equal(1, ecr.Endpoints.Count());

            var endpoint = ecr.Endpoints.First();
            Assert.Equal(2, endpoint.ParameterCount);
            Assert.Equal("greeting", endpoint.GetParameter(0).Name);
        }

        [Fact]
        async public Task ScriptCanUseParams()
        {
            var ecr = CreateEndpointWithScript();
            var endpoint = ecr.Endpoints.Single();
            var responseCreator = endpoint.Responses.Single().Item2;
            Assert.NotNull(responseCreator);
            var response = new TestableHttpResponse();
            var responseBytes = await responseCreator.CreateResponseAsync(
                new TestableHttpRequest("/", null),
                new byte[0], 
                response, 
                endpoint
            );
            Assert.Equal("Hello World", response.WrittenContent);
        }

        [Fact]
        async public Task UpdatedParamsReflectedInScripts()
        {
            var ecr = CreateEndpointWithScript();
            var endpoint = ecr.Endpoints.Single();

            endpoint.GetParameter("greeting").Value = "Goodbye";
            endpoint.GetParameter("subject").Value = "Cruel World";

            var responseCreator = endpoint.Responses.Single().Item2;
            var response = new TestableHttpResponse();
            var responseBytes = await responseCreator.CreateResponseAsync(
                new TestableHttpRequest("/", null),
                new byte[0],
                response,
                endpoint
            );
            Assert.Equal("Goodbye Cruel World", response.WrittenContent);
        }

        public void Dispose()
        {
            dc.Dispose();
        }
    }

    public class TestableHttpResponse : IHttpResponseWrapper
    {
        private string contentType;
        public Stream Body
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public string ContentType
        {
            set
            {
                contentType = value;
            }

            get
            {
                return contentType;
            }
        }

        public HttpStatusCode HttpStatusCode { get; set; }
        public string WrittenContent;

        public async Task WriteAsync(string content, Encoding encoding)
        {
            await Task.Yield(); // to supress warning
            WrittenContent = content;
        }
    }

    public class TestableHttpRequest : IHttpRequestWrapper
    {
        private string path;
        private string queryString;

        public TestableHttpRequest(string path, string queryString)
        {
            this.path = path;
            this.queryString = queryString;
        }

        public string PathAsString { get; set; }
        public string QueryStringAsString { get; set; }
        public IHeaderDictionary Headers => null;

        public PathString Path => new PathString(path);

        public QueryString QueryString => new QueryString(queryString);
    }
}
