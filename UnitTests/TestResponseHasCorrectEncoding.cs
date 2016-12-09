using netmockery;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace UnitTests
{
    public class TestResponseHasCorrectEncoding : WebTestBase, IDisposable
    {
        DirectoryCreator dc;
        EndpointCollectionProvider ecp;
        public TestResponseHasCorrectEncoding()
        {
            dc = new DirectoryCreator();
            dc.AddFile("endpoint1\\endpoint.json", JsonConvert.SerializeObject(DataUtils.CreateSimpleEndpoint("endpoint1", "myfile.txt", "/endpoint1")));
            dc.AddFile("endpoint1\\myfile.txt", "æøå");

            var endpoint2 = DataUtils.CreateSimpleEndpoint("endpoint2", "myfile.txt", "/endpoint2");
            endpoint2.responses[0].charset = "utf-8";
            dc.AddFile("endpoint2\\endpoint.json", JsonConvert.SerializeObject(endpoint2));
            dc.AddFile("endpoint2\\myfile.txt", "æøå");
            var tests = new List<JSONTest>(new[] {
                new JSONTest { name = "Test endpoint1", requestpath = "/endpoint1", expectedresponsebody = "æøå" }
            });
            dc.AddFile("tests\\tests.json", JsonConvert.SerializeObject(tests));

            ecp = new EndpointCollectionProvider(dc.DirectoryName);
            CreateServerAndClient();
        }

        public void Dispose()
        {
            dc.Dispose();
        }

        public override EndpointCollectionProvider GetEndpointCollectionProvider()
        {
            return ecp;
        }

        [Fact]
        async public Task ResponseHasLatin1EncodingIfNotConfigured()
        {
            var response = await client.GetAsync("/endpoint1");
            var bytes = await response.Content.ReadAsByteArrayAsync();
            var str = Encoding.GetEncoding("ISO-8859-1").GetString(bytes);
            Assert.Equal("æøå", str);

            var utf8string = Encoding.UTF8.GetString(bytes);
            Assert.NotEqual("æøå", utf8string);
        }

        [Fact]
        async public Task ResponseCanBeUTF8IfConfigured()
        {
            var response = await client.GetAsync("/endpoint2");
            var bytes = await response.Content.ReadAsByteArrayAsync();
            var str = Encoding.GetEncoding("ISO-8859-1").GetString(bytes);
            Assert.NotEqual("æøå", str);

            var utf8string = Encoding.UTF8.GetString(bytes);
            Assert.Equal("æøå", utf8string);
        }

        [Fact]
        async public Task ResponseHasCharsetInHeader()
        {
            var response = await client.GetAsync("/endpoint1");
            Assert.Equal("text/plain", response.Content.Headers.ContentType.MediaType);
            Assert.Equal("iso-8859-1", response.Content.Headers.ContentType.CharSet);

            response = await client.GetAsync("/endpoint2");
            Assert.Equal("text/plain", response.Content.Headers.ContentType.MediaType);
            Assert.Equal("utf-8", response.Content.Headers.ContentType.CharSet);
        }

        //[Fact]
        

        //TODO: Implement test to check that test cases work as expected
    }
}
