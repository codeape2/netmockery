﻿using Microsoft.AspNetCore.Mvc.Testing;
using netmockery;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace UnitTests
{
    public class TestReloadWorks : IDisposable
    {
        DirectoryCreator dc;
        HttpClient client;

        public TestReloadWorks()
        {
            dc = new DirectoryCreator();
            dc.AddFile(
                "endpoint1/endpoint.json",
                JsonConvert.SerializeObject(DataUtils.CreateSimpleEndpoint("foobar", "myfile.txt"))
            );
            dc.AddFile("endpoint1/myfile.txt", "Hello world");
            var ecp = new EndpointCollectionProvider(dc.DirectoryName);

            var factory = new CustomWebApplicationFactory<Program>(ecp);
            client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });
        }

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
                "endpoint2/endpoint.json",
                JsonConvert.SerializeObject(DataUtils.CreateSimpleEndpoint("baz", "myfile.txt"))
            );
            dc.AddFile("endpoint2/myfile.txt", "Hello world");

            Assert.Equal(new[] { "foobar" }, await GetEndpointNames());

            var response = await client.GetAsync("/__netmockery/endpoints/reloadconfig");
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

            Assert.Equal(new[] { "baz", "foobar" }, await GetEndpointNames());
        }

        private async Task<string[]> GetEndpointNames()
        {
            var response = await client.GetAsync("/__netmockery/endpoints/endpointnames");
            response.EnsureSuccessStatusCode();
            return (from arrayitem in JsonConvert.DeserializeObject<string[]>(await response.Content.ReadAsStringAsync()) orderby arrayitem select arrayitem).ToArray();
        }
    }
}