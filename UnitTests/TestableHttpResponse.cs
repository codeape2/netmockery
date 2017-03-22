using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using netmockery;
using System.IO;
using System.Net;
using System.Text;

namespace UnitTests
{
    public class TestableHttpResponse : IHttpResponseWrapper
    {
        private string contentType;
        public Stream Body
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public string ContentType
        {
            set
            {
                contentType = value;
            }

            get
            {
                return contentType;
            }
        }

        public HttpStatusCode HttpStatusCode { get; set; }
        public string WrittenContent;

        public async Task WriteAsync(string content, Encoding encoding)
        {
            await Task.Yield(); // to supress warning
            WrittenContent = content;
        }
    }
}
