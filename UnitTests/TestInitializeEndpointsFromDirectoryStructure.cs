﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using netmockery;

namespace UnitTests
{
    public class TestInitializeEndpointsFromDirectoryStructure : IDisposable
    {
        DirectoryCreator dc = new DirectoryCreator();

        public TestInitializeEndpointsFromDirectoryStructure()
        {
            dc.AddFile("endpoint1/endpoint.json", "{'name': 'Endpoint1', pathregex: '^/ep1/', responses: []}");
            dc.AddFile("endpoint2/endpoint.json", TestInitFromJSON.ENDPOINTJSON);
        }

        [Fact]
        public void EndpointsInitializedCorrectly()
        {
            var endpointCollection = EndpointCollectionReader.ReadFromDirectory(dc.DirectoryName);
            Assert.Equal(dc.DirectoryName, endpointCollection.SourceDirectory);
            Assert.Equal(2, endpointCollection.Endpoints.Count());
            var ep0 = endpointCollection.Get("Endpoint1");
            Assert.Equal("Endpoint1", ep0.Name);
            Assert.Equal("^/ep1/", ep0.PathRegex);
            Assert.Empty(ep0.Responses);
            Assert.Equal(Path.Combine(dc.DirectoryName, "endpoint1"), ep0.Directory);

            var ep1 = endpointCollection.Get("foo");
            Assert.Equal(2, ep1.Responses.Count());
        }

        public void Dispose()
        {
            dc.Dispose();
        }
    }

    public class TestInitializeEndpointsWithNonEndpointDirectory : IDisposable
    {
        DirectoryCreator dc = new DirectoryCreator();

        public TestInitializeEndpointsWithNonEndpointDirectory()
        {
            dc.AddFile("endpoint1/endpoint.json", "{'name': 'Endpoint1', pathregex: '^/ep1/', responses: []}");
            dc.AddFile("kith/file.txt", "Ignore me!");
        }

        [Fact]
        public void CanLoadOK()
        {
            var endpointCollection = EndpointCollectionReader.ReadFromDirectory(dc.DirectoryName);
            Assert.Single(endpointCollection.Endpoints);
        }

        public void Dispose()
        {
            dc.Dispose();
        }

    }
}
