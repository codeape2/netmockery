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
            var responseCreator = CreateJsonResponse().Validated().CreateResponseCreator(new Endpoint("foo", "bar")) as SimpleResponseCreator;
            Assert.Equal(200, responseCreator.StatusCode);
        }

        [Fact]
        public void CanSetStatusCode()
        {
            var responseCreator = CreateJsonResponse(404).Validated().CreateResponseCreator(new Endpoint("foo", "bar")) as SimpleResponseCreator;
            Assert.NotEqual(200, responseCreator.StatusCode);
            Assert.Equal(404, responseCreator.StatusCode);
        }

        [Fact]
        public void CanUseCustomCodes()
        {
            var responseCreator = CreateJsonResponse(422).Validated().CreateResponseCreator(new Endpoint("foo", "bar")) as SimpleResponseCreator;
            Assert.NotEqual(200, responseCreator.StatusCode);
            Assert.Equal(422, responseCreator.StatusCode);
        }

        [Fact]
        public void ReadsFromEndpointJson()
        {
            var endpointCollection = EndpointCollectionReader.ReadFromDirectory("examples\\example1");

            var endpoint3 = endpointCollection.Resolve("/statuscode");
            Assert.Equal("StatusCodes", endpoint3.Name);

            var responseCreators = from t in endpoint3.Responses select (SimpleResponseCreator)t.Item2;
            Assert.All(responseCreators, r => Assert.Equal("text/plain", r.ContentType));
            Assert.Equal(new[] { 200, 404, 499 }, from r in responseCreators select (int) r.StatusCode);
        }

        [Fact]
        public void FailingTestCase()
        {
            Assert.False(new NetmockeryTestCase().HasExpectations);

            var testcase = new NetmockeryTestCase
            {
                RequestPath = "/statuscode/200",
                ExpectedStatusCode = 404
            };

            Assert.True(testcase.HasExpectations);
            var result = testcase.Execute(EndpointCollectionReader.ReadFromDirectory("examples\\example1"), handleErrors: false);
            Assert.False(result.OK);
            Assert.Equal("Expected http status code: 404\nActual: 200", result.Message);
        }

        [Fact]
        public void PassingTestCase()
        {
            var testcase = new NetmockeryTestCase
            {
                RequestPath = "/statuscode/404",
                ExpectedStatusCode = 404
            };

            Assert.True(testcase.HasExpectations);
            var result = testcase.Execute(EndpointCollectionReader.ReadFromDirectory("examples\\example1"), handleErrors: false);
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
                retval.statuscode = statuscode.ToString();
            }

            return retval;
        }
    }
}
