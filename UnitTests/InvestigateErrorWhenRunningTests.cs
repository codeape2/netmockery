﻿using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.Text;
using netmockery;
using Xunit;

namespace UnitTests
{
    public class InvestigateErrorWhenRunningTests : WebTestBase
    {
        public override EndpointCollectionProvider GetEndpointCollectionProvider() => new EndpointCollectionProvider("examples/example1");

        [Fact(Skip = "Interactive")]        
        public void LetsTry()
        {
            CreateWebHostBuilder().UseKestrel().Build().Run();
        }
    }
}
