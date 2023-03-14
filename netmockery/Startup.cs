using System;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.Text;
using System.IO;
using Microsoft.Extensions.DependencyInjection;

namespace netmockery
{
    public class Startup
    {
        static public bool TestMode { get; set; } = false;

        public void Configure(IApplicationBuilder app)
        {
            var responseRegistry = app.ApplicationServices.GetService<ResponseRegistry>();
            var endpointCollectionProvider = app.ApplicationServices.GetService<EndpointCollectionProvider>();

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
                await context.Request.Body.CopyToAsync(memoryStream);
                var requestBodyBytes = memoryStream.ToArray();
                var requestBody = Encoding.UTF8.GetString(requestBodyBytes);

                if (context.Request.Path.ToString() == "/")
                {
                    context.Response.Redirect("/__netmockery/Home");
                }
                else
                {
                    await HandleRequest(responseRegistry, endpointCollectionProvider, context, requestBody, requestBodyBytes);
                }
            });
        }

        public async Task HandleRequest(ResponseRegistry responseRegistry, EndpointCollectionProvider ecp, HttpContext context, string requestBody, byte[] requestBodyBytes)
        {
            Debug.Assert(context != null);
            var responseRegistryItem = new ResponseRegistryItem
            {
                Timestamp = DateTime.Now,
                RequestBody = requestBody,
                Method = context.Request.Method,
                RequestPath = context.Request.Path.ToString(),
                QueryString = context.Request.QueryString.ToString()
            };
            Debug.Assert(responseRegistryItem.Id == 0);

            try
            {
                await HandleRequestInner(responseRegistry, ecp, responseRegistryItem, context, requestBody, requestBodyBytes);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                responseRegistryItem.Error = e.ToString();
                if (! responseRegistryItem.HasBeenAddedToRegistry)
                {
                    responseRegistry.AddAndWriteIncomingInfoToConsole(responseRegistryItem);
                }
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync( "*********************\n");
                await context.Response.WriteAsync($"NETMOCKERY EXCEPTION:\n\n{e}");
            }
            finally
            {
                if (responseRegistryItem.HasBeenAddedToRegistry)
                {
                    responseRegistryItem.WriteResolvedInfoToConsole();
                }
            }

        }

        public async Task HandleRequestInner(ResponseRegistry responseRegistry, EndpointCollectionProvider ecp, ResponseRegistryItem responseRegistryItem, HttpContext context, string requestBody, byte[] requestBodyBytes)
        {
            Debug.Assert(ecp != null);
            Debug.Assert(responseRegistryItem != null);
            var endpointCollection = ecp.EndpointCollection;
            var endpoint = endpointCollection.Resolve(context.Request.Path.ToString());
            responseRegistryItem.Endpoint = endpoint;
            if (endpoint != null)
            {                
                var matcher_and_creator = endpoint.Resolve(context.Request.Method, context.Request.Path, context.Request.QueryString, requestBody, context.Request.Headers);
                if (matcher_and_creator != null)
                {
                    if (endpoint.RecordRequests)
                    {
                        responseRegistry.AddAndWriteIncomingInfoToConsole(responseRegistryItem);
                    }                    
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
                    responseRegistry.AddAndWriteIncomingInfoToConsole(responseRegistryItem);
                    responseRegistryItem.Error = "Endpoint has no match for request";
                }
            }
            else
            {
                responseRegistry.AddAndWriteIncomingInfoToConsole(responseRegistryItem);
                responseRegistryItem.Error = "No endpoint matches request path";
            }
        }
    }
}
