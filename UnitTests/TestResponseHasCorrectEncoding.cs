using Microsoft.AspNetCore.Mvc.Testing;
using netmockery;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace UnitTests
{
    public class TestResponseHasCorrectEncoding : IDisposable
    {
        DirectoryCreator _dc;
        HttpClient _client;

        public TestResponseHasCorrectEncoding()
        {
            _dc = new DirectoryCreator();
            _dc.AddFile("endpoint1/endpoint.json", JsonConvert.SerializeObject(DataUtils.CreateSimpleEndpoint("endpoint1", "myfile.txt", "/endpoint1")));
            _dc.AddFile("endpoint1/myfile.txt", "æøå");

            var endpoint2 = DataUtils.CreateSimpleEndpoint("endpoint2", "myfile.txt", "/endpoint2");
            endpoint2.responses[0].charset = "latin1";
            _dc.AddFile("endpoint2/endpoint.json", JsonConvert.SerializeObject(endpoint2));
            _dc.AddFile("endpoint2/myfile.txt", "æøå");
            var tests = new List<JSONTest>(new[] {
                new JSONTest { name = "Test endpoint1", requestpath = "/endpoint1", expectedresponsebody = "æøå" }
            });
            _dc.AddFile("tests/tests.json", JsonConvert.SerializeObject(tests));

            var ecp = new EndpointCollectionProvider(_dc.DirectoryName);

            var factory = new CustomWebApplicationFactory<Program>(ecp);
            _client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });
        }

        public void Dispose()
        {
            _dc.Dispose();
        }

        [Fact]
        async public Task ResponseHasUtf8EncodingIfNotConfigured()
        {
            var response = await _client.GetAsync("/endpoint1");
            var bytes = await response.Content.ReadAsByteArrayAsync();
            Assert.Equal("æøå", DecodeUtf8(bytes));
            Assert.NotEqual("æøå", DecodeLatin1(bytes));
        }

        private string DecodeLatin1(byte[] bytes)
        {
            return Encoding.GetEncoding("ISO-8859-1").GetString(bytes);
        }

        private string DecodeUtf8(byte[] bytes)
        {
            return Encoding.UTF8.GetString(bytes);
        }

        [Fact]
        async public Task ResponseCanBeLatin1IfConfigured()
        {
            var response = await _client.GetAsync("/endpoint2");
            var bytes = await response.Content.ReadAsByteArrayAsync();
            Assert.Equal("æøå", DecodeLatin1(bytes));
            Assert.NotEqual("æøå", DecodeUtf8(bytes));
        }

        [Fact]
        async public Task ResponseHasCharsetInHeader()
        {
            var response = await _client.GetAsync("/endpoint1");
            Assert.Equal("text/plain", response.Content.Headers.ContentType.MediaType);
            Assert.Equal("utf-8", response.Content.Headers.ContentType.CharSet);

            response = await _client.GetAsync("/endpoint2");
            Assert.Equal("text/plain", response.Content.Headers.ContentType.MediaType);
            Assert.Equal("iso-8859-1", response.Content.Headers.ContentType.CharSet);
        }
    }
}