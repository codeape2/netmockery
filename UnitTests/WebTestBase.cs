using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.TestHost;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using netmockery;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
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

        private PortableExecutableReference MetadataReferenceForTypesAssembly(Type type)
        {
            var assembly = type.GetTypeInfo().Assembly;
            return MetadataReference.CreateFromFile(assembly.Location);
        }

        public IWebHostBuilder CreateWebHostBuilder()
        {
            IWebHostBuilder webhostBuilder = new WebHostBuilder();
            webhostBuilder.ConfigureServices(InitialServiceConfiguration);

            // thanks to https://github.com/aspnet/Hosting/issues/954
            webhostBuilder = webhostBuilder
                .UseContentRoot("../../../../netmockery")
                .ConfigureLogging(factory => { factory.AddConsole();  })
                .UseStartup<Startup>()
                .ConfigureServices(services => 
                {
                    services.Configure((RazorViewEngineOptions options) =>
                    {
                        var previous = options.CompilationCallback;
                        options.CompilationCallback = (context) =>
                        {
                            previous?.Invoke(context);

                            var assembly = typeof(Startup).GetTypeInfo().Assembly;
                            var assemblies = assembly.GetReferencedAssemblies().Select(
                                x => MetadataReference.CreateFromFile(Assembly.Load(x).Location)
                            ).ToList();

                            assemblies.Add(MetadataReferenceForTypesAssembly(typeof(Microsoft.AspNetCore.Html.IHtmlContent)));
                            assemblies.Add(MetadataReferenceForTypesAssembly(typeof(Microsoft.AspNetCore.Razor.RazorTemplateEngine)));
                            assemblies.Add(MetadataReferenceForTypesAssembly(typeof(Microsoft.AspNetCore.Razor.Runtime.TagHelpers.ITagHelperDescriptorFactory)));
                            assemblies.Add(MetadataReferenceForTypesAssembly(typeof(System.Text.Encodings.Web.HtmlEncoder)));
                            
                            assemblies.Add(MetadataReference.CreateFromFile(Assembly.Load(new AssemblyName("mscorlib")).Location));
                            assemblies.Add(MetadataReference.CreateFromFile(Assembly.Load(new AssemblyName("System.Private.Corelib")).Location));
                            assemblies.Add(MetadataReference.CreateFromFile(Assembly.Load(new AssemblyName("Microsoft.AspNetCore.Razor")).Location));

                            context.Compilation = context.Compilation.AddReferences(assemblies);
                        };
                    });
                });
            return webhostBuilder;
        }

        public void InitialServiceConfiguration(IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient(serviceProvider => GetEndpointCollectionProvider());
        }

        public abstract EndpointCollectionProvider GetEndpointCollectionProvider();
    }
}
