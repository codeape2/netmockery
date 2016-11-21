using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace netmockery
{
    //TODO: Merge with TestRunner?
    public class EndpointTestDefinition
    {
        private NetmockeryTestCase[] testcases;
        public EndpointTestDefinition(IEnumerable<NetmockeryTestCase> testcases)
        {
            this.testcases = testcases.ToArray();
        }

        public IEnumerable<NetmockeryTestCase> Tests => testcases;

        static public bool HasTestSuite(string directory)
        {
            return File.Exists(tests_json_filename(directory));
        }

        static private string tests_directory(string directory) => Path.Combine(directory, "tests");

        public void SetStaticTimeIfConfigured(string directory)
        {
            if (File.Exists(now_txt_filename(directory)))
            {
                var contents = File.ReadAllText(now_txt_filename(directory));
                var datetime = DateTime.ParseExact(contents, "yyyy-MM-dd HH:mm:ss", null);
                RequestInfo.SetStaticNow(datetime);
            }
        }

        static private string tests_json_filename(string directory) => Path.Combine(tests_directory(directory), "tests.json");
        static private string now_txt_filename(string directory) => Path.Combine(tests_directory(directory), "now.txt");

        public static EndpointTestDefinition ReadFromDirectory(string directory)
        {
            var jsonTests = JsonConvert.DeserializeObject<List<JSONTest>>(File.ReadAllText(tests_json_filename(directory)));
            return new EndpointTestDefinition(from jsontest in jsonTests select jsontest.Validated().CreateTestCase(tests_directory(directory)));
        }
    }
}
