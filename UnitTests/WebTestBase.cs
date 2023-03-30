using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using netmockery;
using Microsoft.AspNetCore.Mvc.Testing;


namespace UnitTests
{
    public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
    {
        private EndpointCollectionProvider _endpointCollectionProvider;

        public CustomWebApplicationFactory(EndpointCollectionProvider endpointCollectionProvider) : base()
        {
            _endpointCollectionProvider = endpointCollectionProvider;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("endpoints", ".."); // EndpointCollectionProvider is mocked anyway

            builder.ConfigureServices(services =>
            {
                services.AddTransient(serviceProvider => _endpointCollectionProvider);
            });
        }
    }
}