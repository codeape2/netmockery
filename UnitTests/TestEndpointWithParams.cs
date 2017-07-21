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

    public class TestEndpointWithParams : IDisposable
    {
        private DirectoryCreator dc = new DirectoryCreator();

        private EndpointCollection CreateEndpointWithScript()
        {
            dc.AddFile("endpoint/endpoint.json", JsonConvert.SerializeObject(DataUtils.CreateScriptEndpoint("endpoint", "script.csscript")));
            dc.AddFile("endpoint/script.csscript", "return GetParam(\"greeting\") + \" \" + GetParam(\"subject\");");
            dc.AddFile("endpoint/params.json", JsonConvert.SerializeObject(new[] {
                new JSONParam { name = "greeting", @default = "Hello", description = "foo" },
                new JSONParam { name = "subject", @default = "World", description = "foo" },
            }));

            return EndpointCollectionReader.ReadFromDirectory(dc.DirectoryName);
        }

        private EndpointCollection CreateEndpointWithParam(JSONParam jsonParam)
        {
            dc.AddFile("endpoint/endpoint.json", JsonConvert.SerializeObject(DataUtils.CreateScriptEndpoint("endpoint", "script.csscript")));
            dc.AddFile("endpoint/script.csscript", "return GetParam(\"greeting\") + \" \" + GetParam(\"subject\");");
            dc.AddFile("endpoint/params.json", JsonConvert.SerializeObject(new[] {
                jsonParam
            }));

            return EndpointCollectionReader.ReadFromDirectory(dc.DirectoryName);
        }

        [Fact]
        public void MissingNameGivesError()
        {
            var ae = Assert.Throws<ArgumentException>(() => {
                CreateEndpointWithParam(new JSONParam { @default = "foo", description = "bar" });
            });
            Assert.Equal("Parameter missing name", ae.Message);      
        }

        [Fact]
        public void InvalidNameGivesError()
        {
            var ae = Assert.Throws<ArgumentException>(() => {
                CreateEndpointWithParam(new JSONParam { name = "æøå", @default = "foo", description = "bar" });
            });
            Assert.Equal("Invalid parameter name: 'æøå'", ae.Message);
        }

        [Fact]
        public void MissingDefaultGivesError()
        {
            var ae = Assert.Throws<ArgumentException>(() => {
                CreateEndpointWithParam(new JSONParam { name = "abc", description = "bar" });
            });

            Assert.Equal("Missing default value for parameter 'abc'", ae.Message);
        }

        [Fact]
        public void MissingDescriptionGivesError()
        {
            var ae = Assert.Throws<ArgumentException>(() => {
                CreateEndpointWithParam(new JSONParam { name = "abc", @default="bar" });
            });
            Assert.Equal("Missing description for parameter 'abc'", ae.Message);
        }


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
}
