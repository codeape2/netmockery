using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using netmockery;
using netmockery.Controllers;

namespace UnitTests
{
    public class TestControllers
    {
        [Fact]
        public void DisplayListOfEndpoints()
        {
            //TODO: add value to the test
            var controller = new EndpointsController(null, null);
            var index = controller.Index();
            Console.WriteLine(index);
        }
    }
}
