using netmockery;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace UnitTests
{
    public class TestCappedResponseRegistry
    {
        [Fact]
        public void CanOnlyAddToCapacity()
        {
            var responseRegistry = new ResponseRegistry
            {
                Capacity = 100
            };
            for (int i = 0; i < 1000; i++)
            {
                responseRegistry.Add(new ResponseRegistryItem());
            }
            Assert.Equal(100, responseRegistry.Responses.Count());
        }

        [Fact]
        public void OnlyLastAddedAreAvailable()
        {
            var responseRegistry = new ResponseRegistry
            {
                Capacity = 100
            };
            for (int i = 0; i < 1000; i++)
            {
                responseRegistry.Add(new ResponseRegistryItem());
            }
            Assert.NotNull(responseRegistry.Get(999));
            Assert.NotNull(responseRegistry.Get(901));
            Assert.ThrowsAny<Exception>(() => responseRegistry.Get(10));
            Assert.ThrowsAny<Exception>(() => responseRegistry.Get(900));
        }
    }
}
