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
        static private ResponseRegistry _responseRegistry;


        static public void ReloadConfig()
        {
            Program.ReloadConfig();
            _responseRegistry = new ResponseRegistry();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            ReloadConfig();

            services.AddTransient(typeof(EndpointCollection), serviceProvider => Program.EndpointCollection);
            services.AddTransient(typeof(ResponseRegistry), serviceProvider => _responseRegistry);

            services.AddMvc();
        }

        public async Task HandleRequest(ResponseRegistryItem responseRegistryItem, HttpContext context, string requestBody, byte[] requestBodyBytes)
        {
            var endpoint = Program.EndpointCollection.Resolve(context.Request.Path.ToString());
            responseRegistryItem.Endpoint = endpoint;
            if (endpoint != null)
            {
                bool singleMatch;
                var matcher_and_creator = endpoint.Resolve(context.Request.Path, requestBody, context.Request.Headers, out singleMatch);
                if (matcher_and_creator != null)
                {
                    var responseCreator = matcher_and_creator.Item2;

                    responseRegistryItem.RequestMatcher = matcher_and_creator.Item1;
                    responseRegistryItem.ResponseCreator = responseCreator;
                    responseRegistryItem.SingleMatch = singleMatch;

                    if (responseCreator.Delay > 0)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(responseCreator.Delay));
                    }
                    var responseBytes = await responseCreator.CreateResponseAsync(context.Request, requestBodyBytes, context.Response, endpoint.Directory);
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
                    template: "__netmockery/{controller=Admin}/{action=Index}"
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
                    var indexFile = Path.Combine(Program.EndpointCollection.SourceDirectory, "index.html");
                    if (File.Exists(indexFile))
                    {
                        var responseCreator = new FileResponse(indexFile) { ContentType = "text/html" };
                        await responseCreator.CreateResponseAsync(context.Request, requestBodyBytes, context.Response, null);
                        return;
                    }
                }
                var responseRegistryItem = new ResponseRegistryItem
                {
                    Timestamp = DateTime.Now,
                    RequestBody = requestBody,
                    RequestPath = context.Request.Path.ToString()
                };

                try
                {
                    await HandleRequest(responseRegistryItem, context, requestBody, requestBodyBytes);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                    responseRegistryItem.Error = e.ToString();
                }
                finally
                {
                    responseRegistryItem.WriteToConsole();
                    _responseRegistry.Add(responseRegistryItem);
                }
            });
            
        }
    }
}
