using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using netmockery;

namespace UnitTests
{
    public static class TESTCOMMAND_CONSTANTS
    {
        public const string ENDPOINTJSON = @"
{
    'name': 'foo',
    'pathregex': '^/foo/$',
    'responses': [
        {
            'match': {'regex': 'test'},
            'file': 'content.txt',
            'contenttype': 'text/plain'
        },
        {
            'match': {},
            'script': 'myscript.csscript',
            'contenttype': 'text/xml',
            'replacements': [
                {'search': 'a', 'replace': 'b'},
                {'search': 'foo', 'replace': 'bar'}
            ]
        }
    ]
}
";

        public const string TESTS = @"
[
    {
        'name': '/foo/ request works',
        'requestpath': '/foo/',
        'requestbody': 'heisann test',
        'expectedresponsebody': 'FOOBARBOOBAR'
    },

    {
        'name': '/foo/ request works',
        'requestpath': '/foo/',
        'requestbody': 'heisann test',
        'expectedresponsebody': 'file:example.txt'
    },

    {
        'name': '/foo/ request works',
        'requestpath': '/foo/',
        'requestbody': 'file:example.txt',
        'expectedresponsebody': 'file:example.txt'
    },

    {
        'name': 'Getnow works',
        'requestpath': '/getnow/',
        'expectedrequestmatcher': 'Any request',
        'expectedresponsebody': '2015-01-01 12:01:31'
    }
]
";
    }

    public class TestTestCommandWithoutTestsuite : IDisposable
    {
        DirectoryCreator dc;
        public TestTestCommandWithoutTestsuite()
        {
            dc = new DirectoryCreator();
            dc.AddFile("endpoint1\\endpoint.json", TESTCOMMAND_CONSTANTS.ENDPOINTJSON);
        }

        public void Dispose()
        {
            dc.Dispose();
        }
        
        [Fact]
        public void CheckIfTestSuiteExists()
        {
            Assert.False(EndpointTestDefinition.HasTestSuite(dc.DirectoryName));
        }

        [Fact]
        public void WorksIfEndpointNamedTest()
        {
            dc.AddFile("tests\\endpoint.json", TESTCOMMAND_CONSTANTS.ENDPOINTJSON);
            Assert.False(EndpointTestDefinition.HasTestSuite(dc.DirectoryName));
        }
    }


    public class TestTestCommand : IDisposable
    {
        DirectoryCreator dc;
        public TestTestCommand()
        {
            dc = new DirectoryCreator();
            dc.AddFile("endpoint1\\endpoint.json", TESTCOMMAND_CONSTANTS.ENDPOINTJSON);
            dc.AddFile("endpoint1\\myscript.csscript", "return \"Hello world\";");
            dc.AddFile("endpoint1\\content.txt", "FOOBARBOOBAR");
            dc.AddFile("tests\\tests.json", TESTCOMMAND_CONSTANTS.TESTS);
            dc.AddFile("tests\\example.txt", "FOOBARBOOBAR");
            dc.AddFile("tests\\now.txt", "2015-01-01 12:01:31");
            dc.AddFile("getnow\\endpoint.json", "{'name': 'GetNow', 'pathregex': '/getnow/', 'responses': [{'match': {}, 'script':'getnow.csscript'}]}");
            dc.AddFile("getnow\\getnow.csscript", "GetNow().ToString(\"yyyy-MM-dd HH:mm:ss\")");
        }

        public void Dispose()
        {
            dc.Dispose();
            RequestInfo.SetDynamicNow();
        }

        [Fact]
        public void DetectsTestSuite()
        {
            Assert.True(EndpointTestDefinition.HasTestSuite(dc.DirectoryName));

        }

        [Fact]
        public void CanReadTestsFromJSONFile()
        {
            var endpointTestDefinition = EndpointTestDefinition.ReadFromDirectory(dc.DirectoryName);
            Assert.Equal(4, endpointTestDefinition.Tests.Count());
            var test = endpointTestDefinition.Tests.ElementAt(0);
            Assert.Equal("/foo/ request works", test.Name);
            Assert.Equal("/foo/", test.RequestPath);
            Assert.Equal("heisann test", test.RequestBody);
            Assert.Equal("FOOBARBOOBAR", test.ExpectedResponseBody);
        }

        [Fact]
        async public void RequestBodyCanBeUnspecified()
        {
            var endpointCollection = EndpointCollectionReader.ReadFromDirectory(dc.DirectoryName);

            var test = new NetmockeryTestCase
            {
                RequestPath = "/foo/",
                ExpectedRequestMatcher = "Any request",
                ExpectedResponseBody = "Hello world"
            };

            var result = await test.ExecuteAsync(endpointCollection, false);
            Assert.True(result.OK, result.Message);
        }

        [Fact]
        public void TestsHaveConstantGetNow()
        {
            var testRunner = new TestRunner(EndpointCollectionReader.ReadFromDirectory(dc.DirectoryName));
            var result = testRunner.ExecuteTestAndOutputResult(3);
            Assert.True(result.OK, result.ResultAsString);
        }

        [Fact]
        public void TestsCanHaveDynamicNow()
        {
            dc.DeleteFile("tests\\now.txt");
            var testRunner = new TestRunner(EndpointCollectionReader.ReadFromDirectory(dc.DirectoryName));
            var result = testRunner.ExecuteTestAndOutputResult(3);
            Assert.True(result.Error, result.ResultAsString);
            Assert.Null(result.Exception);
        }

        [Fact]
        public void SetStaticGetNow()
        {
            RequestInfo.SetStaticNow(new DateTime(2015, 6, 7, 8, 9, 10));
            Assert.Equal("2015-06-07 08:09:10", TestDynamicResponse.Eval("GetNow().ToString(\"yyyy-MM-dd HH:mm:ss\")"));
        }


        [Fact]
        public void RequestBodyCanBeReadFromFile()
        {
            var endpointTestDefinition = EndpointTestDefinition.ReadFromDirectory(dc.DirectoryName);
            var test = endpointTestDefinition.Tests.ElementAt(2);
            Assert.Equal("FOOBARBOOBAR", test.RequestBody);
        }

        [Fact]
        async public void CanExecuteTest()
        {
            var endpointTestDefinition = EndpointTestDefinition.ReadFromDirectory(dc.DirectoryName);
            var test = endpointTestDefinition.Tests.ElementAt(0);

            var result = await test.ExecuteAsync(EndpointCollectionReader.ReadFromDirectory(dc.DirectoryName), handleErrors: false);
            Assert.True(result.OK);
        }

        [Fact]
        async public void CanReadExpectedResponseBodyFromFile()
        {
            var endpointTestDefinition = EndpointTestDefinition.ReadFromDirectory(dc.DirectoryName);
            var test = endpointTestDefinition.Tests.ElementAt(1);

            var result = await test.ExecuteAsync(EndpointCollectionReader.ReadFromDirectory(dc.DirectoryName), handleErrors: false);
            Assert.True(result.OK, result.Message);
        }

        [Fact]
        async public void CanCheckExpectedRequestMatcherError()
        {
            var testcase = 
                (new JSONTest { name="checksomething", requestpath = "/foo/", requestbody = "foobar", expectedrequestmatcher = "Regex 'test'" })
                .Validated().CreateTestCase(".");

            Assert.True(testcase.HasExpectations);
            Assert.False(testcase.NeedsResponseBody);
            Assert.Equal("Regex 'test'", testcase.ExpectedRequestMatcher);
            Assert.Equal("foobar", testcase.RequestBody);

            var result = await testcase.ExecuteAsync(EndpointCollectionReader.ReadFromDirectory(dc.DirectoryName));
            Assert.True(result.Error);
            Assert.Null(result.Exception);
            Assert.Equal("Expected request matcher: Regex 'test'\nActual: Any request", result.Message);
        }

        [Fact]
        async public void CanCheckExpectedRequestMatcherSuccess()
        {
            var testcase =
                (new JSONTest { name = "checksomething", requestpath = "/foo/", requestbody = "this is a test", expectedrequestmatcher = "Regex 'test'" })
                .Validated().CreateTestCase(".");
            var result = await testcase.ExecuteAsync(EndpointCollectionReader.ReadFromDirectory(dc.DirectoryName));
            Assert.True(result.OK);
        }

        [Fact]
        async public void CanGetResultBody()
        {
            var testcase =
                (new JSONTest { name = "checksomething", requestpath = "/foo/", requestbody = "this is a test", expectedrequestmatcher = "Regex 'test'" })
                .Validated().CreateTestCase(".");
            var result = await testcase.GetResponseAsync(EndpointCollectionReader.ReadFromDirectory(dc.DirectoryName));
            Assert.Equal("FOOBARBOOBAR", result.Item1);
            Assert.Null(result.Item2);
        }

        [Fact]
        async public void CanCheckExpectedResponseCreatorError()
        {
            var testcase =
                (new JSONTest { name = "checksomething", requestpath = "/foo/", requestbody = "foobar", expectedresponsecreator = "File content.txt" })
                .Validated().CreateTestCase(".");
            Assert.True(testcase.HasExpectations);
            Assert.False(testcase.NeedsResponseBody);
            Assert.Equal("File content.txt", testcase.ExpectedResponseCreator);
            var result = await testcase.ExecuteAsync(EndpointCollectionReader.ReadFromDirectory(dc.DirectoryName));
            Assert.True(result.Error);
            Assert.Equal("Expected response creator: File content.txt\nActual: Execute script myscript.csscript", result.Message);
        }

        [Fact]
        async public void CanCheckExpectedResponseCreatorSuccess()
        {
            var testcase =
                (new JSONTest { name = "checksomething", requestpath = "/foo/", requestbody = "this is a test", expectedresponsecreator = "File content.txt" })
                .Validated().CreateTestCase(".");
            Assert.Equal("File content.txt", testcase.ExpectedResponseCreator);
            var result = await testcase.ExecuteAsync(EndpointCollectionReader.ReadFromDirectory(dc.DirectoryName));
            Assert.True(result.OK, result.Message);
            Assert.Null(result.Message);
        }

        [Fact]
        public void CanReadQueryString()
        {
            var testcase = (new JSONTest { querystring = "?foo=bar" }).CreateTestCase(".");
            Assert.Equal("?foo=bar", testcase.QueryString);
        }

        [Fact]
        async public void CanExecuteWithQueryStringFailure()
        {
            var testcase =
                (new JSONTest { name = "checksomething", requestpath = "/foo/", querystring = "?a=test", requestbody = "foobar", expectedresponsecreator = "File content.txt" })
                .Validated().CreateTestCase(".");
            var result = await testcase.ExecuteAsync(EndpointCollectionReader.ReadFromDirectory(dc.DirectoryName));
            Assert.True(result.OK, result.Message);
            Assert.Null(result.Message);
        }
    }
}
