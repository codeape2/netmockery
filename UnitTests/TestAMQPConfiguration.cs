using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using netmockery;


namespace UnitTests
{
    public class TestAMQPConfiguration : IDisposable
    {
        DirectoryCreator dc;

        public TestAMQPConfiguration()
        {
            dc = new DirectoryCreator();
            dc.AddFile("amqp.json", "{ 'uri': 'amqp://localhost:9876/'}");
            dc.AddFile("q1\\queue.json", "{ 'address': 'q1'}");
            dc.AddFile("q2\\queue.json", "{ 'address': 'q2'}");
        }

        public void Dispose()
        {
            dc.Dispose();
        }

        [Fact]
        public void CanReadAMQPMainConfigFromDirectory()
        {
            var amqpConfig = AMQPConfig.ReadFromDirectory(dc.DirectoryName);
            Assert.Equal("amqp://localhost:9876/", amqpConfig.Uri.ToString());
        }

        [Fact]
        public void CanReadQueueConfigFromDirectory()
        {
            var amqpConfig = AMQPConfig.ReadFromDirectory(dc.DirectoryName);
            Assert.Equal(new[] { "q1", "q2" }, amqpConfig.QueueAddresses);
        }
    }
}
