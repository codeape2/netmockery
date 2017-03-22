using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using netmockery;
using Microsoft.AspNetCore.Http;

namespace UnitTests
{
    public class TestableHttpRequest : IHttpRequestWrapper
    {
        private string path;
        private string queryString;

        public TestableHttpRequest(string path, string queryString)
        {
            this.path = path;
            this.queryString = queryString;
        }

        public string PathAsString { get; set; }
        public string QueryStringAsString { get; set; }
        public IHeaderDictionary Headers => null;

        public PathString Path => new PathString(path);

        public QueryString QueryString => new QueryString(queryString);
    }
}
