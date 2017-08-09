using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using netmockery;
using Newtonsoft.Json;
using System.Linq;

namespace UnitTests
{
    public class TestJSONAdditionalData
    {
        [Fact]
        public void NoAdditionalDataGivesNullInJSONEndpoint()
        {
            var endpoint = JsonConvert.DeserializeObject<JSONEndpoint>("{\"name\": \"foobar\"}");
            Assert.Equal("foobar", endpoint.name);
            Assert.Null(endpoint.AdditionalData);

            // check no exception
            endpoint.ThrowExceptionIfAdditionalData();
        }

        [Fact]
        public void AdditionalDataIsDeserializedJSONEndpoint()
        {
            var endpoint = JsonConvert.DeserializeObject<JSONEndpoint>("{\"name\": \"foobar\", \"nmae\": \"foobar\"}");
            Assert.Equal("foobar", endpoint.name);
            Assert.NotNull(endpoint.AdditionalData);
            Assert.Equal("nmae", endpoint.AdditionalData.Keys.Single());
        }

        [Fact]
        public void AdditionalDataIsNotValid()
        {
            var endpoint = JsonConvert.DeserializeObject<JSONEndpoint>("{\"name\": \"foobar\", \"nmae\": \"foobar\"}");
            Assert.Equal("foobar", endpoint.name);
            Assert.NotNull(endpoint.AdditionalData);
            Assert.Equal("nmae", endpoint.AdditionalData.Keys.Single());

            var exception = Assert.Throws<ArgumentException>(() => endpoint.ThrowExceptionIfAdditionalData());
        }


        [Fact]
        public void NullAdditionalDataIsNotSerialized()
        {
            var endpoint = JsonConvert.DeserializeObject<JSONEndpoint>("{\"name\": \"foobar\"}");
            Assert.Null(endpoint.AdditionalData);

            var as_str = JsonConvert.SerializeObject(endpoint);
            Assert.Equal("{\"name\":\"foobar\",\"pathregex\":null,\"responses\":null}", as_str);
        }
    }
}
