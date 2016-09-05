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
            var adminController = new AdminController(null, null);
            var index = adminController.Index();
            Console.WriteLine(index);
        }
    }
}
