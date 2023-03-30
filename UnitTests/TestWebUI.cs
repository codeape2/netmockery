using Microsoft.AspNetCore.Mvc.Testing;
using netmockery;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace UnitTests
{
    public class TestWebUI
    {
        private HttpClient client;
        private EndpointCollectionProvider ecp;

        public TestWebUI(ITestOutputHelper output)
        {
            ecp = new EndpointCollectionProvider("examples/example1");

            var factory = new CustomWebApplicationFactory<Program>(ecp);
            client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });
        }

        [Fact]
        public async Task RootRedirectsToHome()
        {
            var response = await client.GetAsync("/");
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("/__netmockery/Home", response.Headers.Location.ToString());
        }

        [Fact]
        public async Task HomeRedirectsToEndpoints()
        {
            var response = await client.GetAsync("/__netmockery/Home");
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("/__netmockery", response.Headers.Location.ToString());
        }
    }
}