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
    public class FileResponseCreatorCanUseParamForFilename
    {
        private Endpoint endpoint;
        private EndpointParameter filenameParam;
        private EndpointParameter contenttypeParam;
        private EndpointParameter fooParam;
        private EndpointParameter delayParam;
        private EndpointParameter statusCodeParam;

        public FileResponseCreatorCanUseParamForFilename()
        {
            endpoint = new Endpoint("foo", "bar");
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

            //TODO: Lag skikkelig feilmelding ved missing param lookup
        }

        [Fact]
        public void ParamNamesMustBeUnique()
        {
            Assert.Throws<ArgumentException>(() => { endpoint.AddParameter(filenameParam); });
        }

        [Fact]
        public void EnsureResultingFileIsInsideEndpointCollectionDirectory()
        {

        }

        [Fact]
        public void CanUseParamForFilename()
        {
            var responseCreator = new FileResponse(".", "$filename", endpoint);
            Assert.Equal(".\\file.txt", responseCreator.Filename);

            filenameParam.Value = "otherfile.txt";
            Assert.Equal(".\\otherfile.txt", responseCreator.Filename);
        }

        [Fact]
        public void CanUseParamForContenttype()
        {
            var responseCreator = new FileResponse(".", "file.txt", endpoint);
            responseCreator.ContentType = "$contenttype";

            Assert.Equal("text/plain", responseCreator.ContentType);
        }

        [Fact]
        public void CanUseParamForLiteral()
        {
            var responseCreator = new LiteralResponse("$foo", endpoint);
            Assert.Equal("FOO", responseCreator.Body);

            fooParam.Value = "BAR";
            Assert.Equal("BAR", responseCreator.Body);
        }

        [Fact]
        public void CanUseParamForScriptFilename()
        {
            var responseCreator = new FileDynamicResponseCreator(".", "$filename", endpoint);
            Assert.Equal(".\\file.txt", responseCreator.Filename);

            filenameParam.Value = "another.txt";

            Assert.Equal(".\\another.txt", responseCreator.Filename);
        }

        [Fact]
        public void CanUseParamForDelay()
        {
            var responseCreator = new FileDynamicResponseCreator(".", "file.txt", endpoint);
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
            Assert.Equal(2, endpoint.Parameters.Count());
            Assert.Equal("greeting", endpoint.Parameters.ElementAt(0).Name);
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
                new TestableHttpRequest { PathAsString = "/" }, 
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

            endpoint.Parameters.Single(p => p.Name == "greeting").Value = "Goodbye";
            endpoint.Parameters.Single(p => p.Name == "subject").Value = "Cruel World";

            var responseCreator = endpoint.Responses.Single().Item2;
            var response = new TestableHttpResponse();
            var responseBytes = await responseCreator.CreateResponseAsync(
                new TestableHttpRequest { PathAsString = "/" },
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
        }

        public HttpStatusCode HttpStatusCode { get; set; }
        public string WrittenContent;

        public async Task WriteAsync(string content, Encoding encoding)
        {
            WrittenContent = content;
        }
    }

    public class TestableHttpRequest : IHttpRequestWrapper
    {
        //TODO: Fix horrible names
        public string PathAsString { get; set; }
        public string QueryStringAsString { get; set; }
        public IHeaderDictionary Headers => null;

        public PathString Path => new PathString(PathAsString);

        public QueryString QueryString => new QueryString(QueryStringAsString);
    }
}
