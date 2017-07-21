using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using netmockery;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace UnitTests
{
    public abstract class WebTestBase
    {
        public TestServer server;
        public HttpClient client;

        public void CreateServerAndClient()
        {
            server = new TestServer(CreateWebHostBuilder());
            client = server.CreateClient();
        }

        public IWebHostBuilder CreateWebHostBuilder()
        {
            IWebHostBuilder webhostBuilder = new WebHostBuilder();
            webhostBuilder.ConfigureServices(InitialServiceConfiguration);
            webhostBuilder = webhostBuilder.UseContentRoot("../../../../netmockery").UseStartup<Startup>();
            return webhostBuilder;
        }

        public void InitialServiceConfiguration(IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient(serviceProvider => GetEndpointCollectionProvider());
        }

        public abstract EndpointCollectionProvider GetEndpointCollectionProvider();
    }
}
