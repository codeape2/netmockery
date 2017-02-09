using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using netmockery;
using System.Net;

namespace UnitTests
{
    public class TestStatusCode
    {
        [Fact]
        public void DefaultIs200()
        {            
            var responseCreator = CreateJsonResponse().Validated().CreateResponseCreator(".") as SimpleResponseCreator;
            Assert.Equal(HttpStatusCode.OK, responseCreator.HttpStatusCode);
        }

        [Fact]
        public void CanSetStatusCode()
        {
            var responseCreator = CreateJsonResponse(404).Validated().CreateResponseCreator(".") as SimpleResponseCreator;
            Assert.NotEqual(HttpStatusCode.OK, responseCreator.HttpStatusCode);
            Assert.Equal(HttpStatusCode.NotFound, responseCreator.HttpStatusCode);
        }

        [Fact]
        public void CanUseCustomCodes()
        {
            var responseCreator = CreateJsonResponse(422).Validated().CreateResponseCreator(".") as SimpleResponseCreator;
            Assert.NotEqual(HttpStatusCode.OK, responseCreator.HttpStatusCode);
            Assert.Equal(422, (int)responseCreator.HttpStatusCode);
        }

        [Fact]
        public void ReadsFromEndpointJson()
        {
            var endpointCollection = EndpointCollectionReader.ReadFromDirectory("examples\\example1");

            var endpoint3 = endpointCollection.Resolve("/statuscode");
            Assert.Equal("StatusCodes", endpoint3.Name);

            var responseCreators = from t in endpoint3.Responses select (SimpleResponseCreator)t.Item2;
            Assert.All(responseCreators, r => Assert.Equal("text/plain", r.ContentType));
            Assert.Equal(new[] { 200, 404, 499 }, from r in responseCreators select (int) r.HttpStatusCode);
        }

        [Fact]
        public async Task FailingTestCase()
        {
            Assert.False(new NetmockeryTestCase().HasExpectations);

            var testcase = new NetmockeryTestCase
            {
                RequestPath = "/statuscode/200",
                ExpectedStatusCode = 404
            };

            Assert.True(testcase.HasExpectations);
            var result = await testcase.ExecuteAsync(EndpointCollectionReader.ReadFromDirectory("examples\\example1"), handleErrors: false);
            Assert.False(result.OK);
            Assert.Equal("Expected http status code: 404\nActual: 200", result.Message);
        }

        [Fact]
        public async Task PassingTestCase()
        {
            var testcase = new NetmockeryTestCase
            {
                RequestPath = "/statuscode/404",
                ExpectedStatusCode = 404
            };

            Assert.True(testcase.HasExpectations);
            var result = await testcase.ExecuteAsync(EndpointCollectionReader.ReadFromDirectory("examples\\example1"), handleErrors: false);
            Assert.True(result.OK);
        }

        JSONResponse CreateJsonResponse(int statuscode = -1)
        {
            var retval = new JSONResponse
            {
                match = new JSONRequestMatcher(),
                literal = "Heisann"
            };

            if (statuscode != -1)
            {
                retval.statuscode = statuscode;
            }

            return retval;
        }
    }
}
