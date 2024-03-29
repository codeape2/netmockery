﻿using System;
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
            'match': {'regex': 'replace'},
            'file': 'content.txt',
            'replacements': [{'search': 'FOO', 'replace': 'XXX'}],
            'contenttype': 'text/plain'
        },

        {
            'match': {},
            'script': 'myscript.csscript',
            'contenttype': 'text/xml',
            'charset': 'ascii',
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
        'requestbody': 'can I replace?',
        'expectedresponsebody': 'XXXBARBOOBAR'
    },

    {
        'name': '/foo/ request works',
        'requestpath': '/foo/',
        'requestbody': 'file:example.txt',
        'expectedresponsecreator': 'Execute script myscript.csscript',
        'expectedresponsebody': 'file:response.txt'
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
            dc.AddFile("endpoint1/endpoint.json", TESTCOMMAND_CONSTANTS.ENDPOINTJSON);
        }

        public void Dispose()
        {
            dc.Dispose();
        }
        
        [Fact]
        public void CheckIfTestSuiteExists()
        {
            Assert.False(TestRunner.HasTestSuite(dc.DirectoryName));
        }

        [Fact]
        public void WorksIfEndpointNamedTest()
        {
            dc.AddFile("tests/endpoint.json", TESTCOMMAND_CONSTANTS.ENDPOINTJSON);
            Assert.False(TestRunner.HasTestSuite(dc.DirectoryName));
        }
    }


    public class TestTestCommand : IDisposable
    {
        DirectoryCreator dc;
        public TestTestCommand()
        {
            dc = new DirectoryCreator();
            dc.AddFile("endpoint1/endpoint.json", TESTCOMMAND_CONSTANTS.ENDPOINTJSON);
            dc.AddFile("endpoint1/myscript.csscript", "return \"Hello world\";");
            dc.AddFile("endpoint1/content.txt", "FOOBARBOOBAR");
            dc.AddFile("tests/tests.json", TESTCOMMAND_CONSTANTS.TESTS);
            dc.AddFile("tests/example.txt", "FOOBARBOOBAR");
            dc.AddFile("tests/response.txt", "Hello world");
            dc.AddFile("tests/now.txt", "2015-01-01 12:01:31");
            dc.AddFile("getnow/endpoint.json", "{'name': 'GetNow', 'pathregex': '/getnow/', 'responses': [{'match': {}, 'script':'getnow.csscript'}]}");
            dc.AddFile("getnow/getnow.csscript", "GetNow().ToString(\"yyyy-MM-dd HH':'mm':'ss\")");
        }

        public void Dispose()
        {
            dc.Dispose();
        }

        [Fact]
        public async Task RunTestsWithReplacement()
        {
            var testRunner = new ConsoleTestRunner(EndpointCollectionReader.ReadFromDirectory(dc.DirectoryName));
            var result = await testRunner.ExecuteTestAndOutputResultAsync(1);
            Assert.True(result.OK, result.ResultAsString);
        }

        [Fact]
        public void DetectsTestSuite()
        {
            Assert.True(TestRunner.HasTestSuite(dc.DirectoryName));

        }

        [Fact]
        public void CanReadTestsFromJSONFile()
        {
            var tests = TestRunner.ReadFromDirectory(dc.DirectoryName);
            Assert.Equal(4, tests.Count());
            var test = tests.ElementAt(0);
            Assert.Equal("/foo/ request works", test.Name);
            Assert.Equal("/foo/", test.RequestPath);
            Assert.Equal("heisann test", test.RequestBody);
            Assert.Equal("FOOBARBOOBAR", test.ExpectedResponseBody);
        }

        [Fact]
        public async Task RequestBodyCanBeUnspecified()
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
        public async Task TestsHaveConstantGetNow()
        {
            var testRunner = new ConsoleTestRunner(EndpointCollectionReader.ReadFromDirectory(dc.DirectoryName));
            var result = await testRunner.ExecuteTestAndOutputResultAsync(3);
            Assert.True(result.OK, result.ResultAsString);
        }

        [Fact]
        public async Task TestsCanHaveDynamicNow()
        {
            dc.DeleteFile("tests/now.txt");
            var testRunner = new ConsoleTestRunner(EndpointCollectionReader.ReadFromDirectory(dc.DirectoryName));
            var result = await testRunner.ExecuteTestAndOutputResultAsync(3);
            Assert.True(result.Error, result.ResultAsString);
            Assert.Null(result.Exception);
        }

        [Fact]
        public async Task SetStaticGetNow()
        {
            Assert.Equal("2015-06-07 08:09:10", await TestDynamicResponse.EvalAsync("GetNow().ToString(\"yyyy-MM-dd HH':'mm':'ss\")", now: new DateTime(2015, 6, 7, 8, 9, 10)));
        }


        [Fact]
        public void RequestBodyCanBeReadFromFile()
        {
            var tests = TestRunner.ReadFromDirectory(dc.DirectoryName);
            var test = tests.ElementAt(2);
            Assert.Equal("FOOBARBOOBAR", test.RequestBody);
        }

        [Fact]
        public async Task TestRunnerKeepsTrackOfTestCoverage()
        {
            var testRunner = new ConsoleTestRunner(EndpointCollectionReader.ReadFromDirectory(dc.DirectoryName));
            await testRunner.TestAllAsync(false, true);

            var coverageInfo = testRunner.GetCoverageInfo();
            Assert.NotNull(coverageInfo);

            Assert.Equal(new[] { "foo", "GetNow" }, coverageInfo.EndpointsCovered);
            Assert.Empty(coverageInfo.EndpointsNotCovered);
        }

        [Fact]
        public async Task TrackTestConverageByResponseRule()
        {
            var testRunner = new ConsoleTestRunner(EndpointCollectionReader.ReadFromDirectory(dc.DirectoryName));
            await testRunner.TestAllAsync(false, false);

            var coverageInfo = testRunner.GetCoverageInfo();
            Assert.NotNull(coverageInfo);

            Assert.Equal(new[] { "foo#0", "GetNow#0" }, coverageInfo.ResponseRulesCovered);
            Assert.Equal(new[] { "foo#1", "foo#2" }, coverageInfo.ResponseRulesNotCovered);
        }

        [Fact]
        public async Task TestRunnerKeepsTrackOfCoverageWhenRunningSingleTest()
        {
            var testRunner = new ConsoleTestRunner(EndpointCollectionReader.ReadFromDirectory(dc.DirectoryName));
            await testRunner.ExecuteTestAndOutputResultAsync(0);

            var coverageInfo = testRunner.GetCoverageInfo();
            Assert.NotNull(coverageInfo);

            Assert.Equal(new[] { "foo" }, coverageInfo.EndpointsCovered);
            Assert.Equal(new[] { "GetNow" }, coverageInfo.EndpointsNotCovered);

        }

        [Fact]
        public async Task CanExecuteTest()
        {
            var endpointCollection = EndpointCollectionReader.ReadFromDirectory(dc.DirectoryName);
            var testRunner = new ConsoleTestRunner(endpointCollection);

            var test = testRunner.Tests.ElementAt(0);
            var result = await test.ExecuteAsync(endpointCollection, handleErrors: false);
            Assert.True(result.OK);
        }

        [Fact]
        public async Task CanReadExpectedResponseBodyFromFile()
        {
            var endpointCollection = EndpointCollectionReader.ReadFromDirectory(dc.DirectoryName);
            var testRunner = new ConsoleTestRunner(endpointCollection);

            var test = testRunner.Tests.ElementAt(2);

            var result = await test.ExecuteAsync(endpointCollection, handleErrors: false);
            Assert.True(result.OK, result.Message);
        }

        [Fact]
        public async Task CanCheckExpectedRequestMatcherError()
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
        public async Task CanCheckExpectedRequestMatcherSuccess()
        {
            var testcase =
                (new JSONTest { name = "checksomething", requestpath = "/foo/", requestbody = "this is a test", expectedrequestmatcher = "Regex 'test'" })
                .Validated().CreateTestCase(".");
            var result = await testcase.ExecuteAsync(EndpointCollectionReader.ReadFromDirectory(dc.DirectoryName));
            Assert.True(result.OK);
        }

        [Fact]
        public async Task CanCheckExpectedContentTypeSuccess()
        {
            var testcase =
                (new JSONTest { name = "checksomething", requestpath = "/foo/", requestbody = "foobar", expectedcontenttype = "text/xml" })
                .Validated().CreateTestCase(".");

            Assert.True(testcase.HasExpectations);
            Assert.True(testcase.NeedsResponseBody);
            Assert.Equal("text/xml", testcase.ExpectedContentType);

            var result = await testcase.ExecuteAsync(EndpointCollectionReader.ReadFromDirectory(dc.DirectoryName));
            Assert.True(result.OK);
        }

        [Fact]
        public async Task CanCheckExpectedContentTypeError()
        {
            var testcase =
                (new JSONTest { name = "checksomething", requestpath = "/foo/", requestbody = "foobar", expectedcontenttype = "text/plain" })
                .Validated().CreateTestCase(".");

            Assert.Equal("text/plain", testcase.ExpectedContentType);

            var result = await testcase.ExecuteAsync(EndpointCollectionReader.ReadFromDirectory(dc.DirectoryName));
            Assert.Null(result.Exception);
            Assert.True(result.Error);            
            Assert.Equal("Expected contenttype: 'text/plain'\nActual: 'text/xml'", result.Message);
        }

        [Fact]
        public async Task CanCheckExpectedCharSetSuccess()
        {
            var testcase =
                (new JSONTest { name = "checksomething", requestpath = "/foo/", requestbody = "foobar", expectedcharset = "us-ascii" })
                .Validated().CreateTestCase(".");

            Assert.True(testcase.HasExpectations);
            Assert.True(testcase.NeedsResponseBody);
            Assert.Equal("us-ascii", testcase.ExpectedCharSet);

            var result = await testcase.ExecuteAsync(EndpointCollectionReader.ReadFromDirectory(dc.DirectoryName));
            Assert.Null(result.Exception);
            Assert.Null(result.Message);
            Assert.True(result.OK);
        }

        [Fact]
        public async Task CanCheckExpectedCharSetError()
        {
            var testcase =
                (new JSONTest { name = "checksomething", requestpath = "/foo/", requestbody = "foobar", expectedcharset = "utf-8" })
                .Validated().CreateTestCase(".");

            Assert.Equal("utf-8", testcase.ExpectedCharSet);

            var result = await testcase.ExecuteAsync(EndpointCollectionReader.ReadFromDirectory(dc.DirectoryName));
            Assert.Null(result.Exception);
            Assert.True(result.Error);
            Assert.Equal("Expected charset: 'utf-8'\nActual: 'us-ascii'", result.Message);
        }

        [Fact]
        public async Task CanGetResultBody()
        {
            var testcase =
                (new JSONTest { name = "checksomething", requestpath = "/foo/", requestbody = "this is a test", expectedrequestmatcher = "Regex 'test'" })
                .Validated().CreateTestCase(".");
            var result = await testcase.GetResponseAsync(EndpointCollectionReader.ReadFromDirectory(dc.DirectoryName), null);
            Assert.Equal("FOOBARBOOBAR", result.Item1);
            Assert.Null(result.Item2);
        }

        [Fact]
        public async Task CanCheckExpectedResponseCreatorError()
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
        public async Task CanCheckExpectedResponseCreatorSuccess()
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
        public async Task CanExecuteWithQueryStringFailure()
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
