using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Text;
using System.IO;

namespace netmockery
{
    public class Startup
    {
        private ResponseRegistry _responseRegistry;
        private EndpointCollectionProvider _endpointCollectionProvider;
        static public bool TestMode { get; set; } = false;

        public ResponseRegistry ResponseRegistry => _responseRegistry;

        public Startup(EndpointCollectionProvider endpointCollectionProvider)
        {
            Debug.Assert(endpointCollectionProvider != null);
            _endpointCollectionProvider = endpointCollectionProvider;
        }

        
        public void ReloadConfig()
        {
            _responseRegistry = new ResponseRegistry();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            ReloadConfig();

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddTransient(typeof(ResponseRegistry), serviceProvider => _responseRegistry);
            services.AddTransient<EndpointCollection>(serviceProvider => serviceProvider.GetService<EndpointCollectionProvider>().EndpointCollection);

            services.AddMvc();
        }

        public async Task HandleRequest(HttpContext context, string requestBody, byte[] requestBodyBytes)
        {
            Debug.Assert(context != null);
            var responseRegistryItem = _responseRegistry.Add(new ResponseRegistryItem
            {
                Timestamp = DateTime.Now,
                RequestBody = requestBody,
                Method = context.Request.Method,
                RequestPath = context.Request.Path.ToString(),
                QueryString = context.Request.QueryString.ToString()
            });
            responseRegistryItem.WriteIncomingInfoToConsole();

            try
            {
                await HandleRequestInner(responseRegistryItem, context, requestBody, requestBodyBytes);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                responseRegistryItem.Error = e.ToString();
            }
            finally
            {
                responseRegistryItem.WriteResolvedInfoToConsole();                
            }

        }

        public async Task HandleRequestInner(ResponseRegistryItem responseRegistryItem, HttpContext context, string requestBody, byte[] requestBodyBytes)
        {
            Debug.Assert(_endpointCollectionProvider != null);
            var endpointCollection = _endpointCollectionProvider.EndpointCollection;
            var endpoint = endpointCollection.Resolve(context.Request.Path.ToString());
            responseRegistryItem.Endpoint = endpoint;
            if (endpoint != null)
            {
                var matcher_and_creator = endpoint.Resolve(context.Request.Method, context.Request.Path, context.Request.QueryString, requestBody, context.Request.Headers);
                if (matcher_and_creator != null)
                {
                    var responseCreator = matcher_and_creator.ResponseCreator;

                    responseRegistryItem.RequestMatcher = matcher_and_creator.RequestMatcher;
                    responseRegistryItem.ResponseCreator = responseCreator;
                    responseRegistryItem.SingleMatch = matcher_and_creator.SingleMatch;

                    if (responseCreator.Delay > 0)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(responseCreator.Delay));
                    }

                    if (TestMode)
                    {
                        context.Response.Headers["X-Netmockery-RequestMatcher"] = matcher_and_creator.RequestMatcher.ToString();
                        context.Response.Headers["X-Netmockery-ResponseCreator"] = matcher_and_creator.ResponseCreator.ToString();
                    }

                    var responseBytes = await responseCreator.CreateResponseAsync(new HttpRequestWrapper(context.Request), requestBodyBytes, new HttpResponseWrapper(context.Response), endpoint);
                    
                    responseRegistryItem.ResponseBody = Encoding.UTF8.GetString(responseBytes);
                }
                else
                {
                    responseRegistryItem.Error = "Endpoint has no match for request";
                }
            }
            else
            {
                responseRegistryItem.Error = "No endpoint matches request path";
            }
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseDeveloperExceptionPage();
            app.UseMvc(
                routes => routes.MapRoute(
                    name: "Default", 
                    template: "__netmockery/{controller=Endpoints}/{action=Index}"
                )
            );

            app.UseStaticFiles();
            
            app.Run(async (context) =>
            {
                var memoryStream = new MemoryStream();
                context.Request.Body.CopyTo(memoryStream);
                var requestBodyBytes = memoryStream.ToArray();
                var requestBody = Encoding.UTF8.GetString(requestBodyBytes);

                if (context.Request.Path.ToString() == "/")
                {
                    context.Response.Redirect("/__netmockery/Home");
                }
                else
                {
                    await HandleRequest(context, requestBody, requestBodyBytes);
                }
            });
            
        }
    }
}
