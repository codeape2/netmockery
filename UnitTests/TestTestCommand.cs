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
            'response': {
                'file': 'content.txt',
                'contenttype': 'text/plain'
            }
        },
        {
            'match': {},
            'response': {
                'script': 'myscript.csscript',
                'contenttype': 'text/xml',
                'replacements': [
                    {'search': 'a', 'replace': 'b'},
                    {'search': 'foo', 'replace': 'bar'}
                ]
            }
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
            dc.AddFile("endpoint1\\content.txt", "FOOBARBOOBAR");
            dc.AddFile("tests\\tests.json", TESTCOMMAND_CONSTANTS.TESTS);
        }

        public void Dispose()
        {
            dc.Dispose();
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
            Assert.Equal(1, endpointTestDefinition.Tests.Count());
            var test = endpointTestDefinition.Tests.ElementAt(0);
            Assert.Equal("/foo/ request works", test.Name);
            Assert.Equal("/foo/", test.RequestPath);
            Assert.Equal("heisann test", test.RequestBody);
            Assert.Equal("FOOBARBOOBAR", test.ExpectedResponseBody);
        }

        [Fact]
        async public void CanExecuteTest()
        {
            var endpointTestDefinition = EndpointTestDefinition.ReadFromDirectory(dc.DirectoryName);
            var test = endpointTestDefinition.Tests.ElementAt(0);

            var result = await test.ExecuteAsync(EndpointCollectionReader.ReadFromDirectory(dc.DirectoryName), handleErrors: false);
            Assert.True(result.OK);
        }
    }
}
