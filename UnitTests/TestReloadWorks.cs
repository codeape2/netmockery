using netmockery;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace UnitTests
{
    public class TestReloadWorks : WebTestBase, IDisposable
    {
        EndpointCollectionProvider ecp;
        DirectoryCreator dc;
        public TestReloadWorks()
        {
            dc = new DirectoryCreator();
            dc.AddFile(
                "endpoint1\\endpoint.json", 
                JsonConvert.SerializeObject(DataUtils.CreateSimpleEndpoint("foobar", "myfile.txt"))
            );
            dc.AddFile("endpoint1\\myfile.txt", "Hello world");
            ecp = new EndpointCollectionProvider(dc.DirectoryName);
            CreateServerAndClient();
        }

        public override EndpointCollectionProvider GetEndpointCollectionProvider() => ecp;

        public void Dispose()
        {
            dc.Dispose();
        }

        [Fact]
        public async Task CanListEndpointnames()
        {
            Assert.Equal(new[] { "foobar" }, await GetEndpointNames());
        }

        [Fact]
        public async Task NewEndpointIsAvailableAfterReload()
        {
            Assert.Equal(new[] { "foobar" }, await GetEndpointNames());

            dc.AddFile(
                "endpoint2\\endpoint.json",
                JsonConvert.SerializeObject(DataUtils.CreateSimpleEndpoint("baz", "myfile.txt"))
            );
            dc.AddFile("endpoint2\\myfile.txt", "Hello world");

            Assert.Equal(new[] { "foobar" }, await GetEndpointNames());

            var response = await client.GetAsync("/__netmockery/endpoints/reloadconfig");
            Assert.Equal(System.Net.HttpStatusCode.Redirect, response.StatusCode);

            Assert.Equal(new[] { "foobar", "baz" }, await GetEndpointNames());
        }

        private async Task<string[]> GetEndpointNames()
        {
            var response = await client.GetAsync("/__netmockery/endpoints/endpointnames");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsAsync<string[]>();
        }
    }
}
