using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace netmockery
{
    public class TestCaseHttpRequest : IHttpRequestWrapper
    {
        private string path;
        private HeaderDictionary headerDictionary;

        public TestCaseHttpRequest(string path)
        {
            this.path = path;
        }
        
        public IHeaderDictionary Headers
        {
            get
            {
                return headerDictionary;
            }
        }

        public PathString Path
        {
            get
            {

                return new PathString(path);
            }
        }
    }

    public class TestCaseHttpResponse : IHttpResponseWrapper
    {
        MemoryStream memoryStream = new MemoryStream();
        string writtenContent;
        Encoding writtenEncoding;
        string contentType;

        public Stream Body
        {
            get
            {
                return memoryStream;
            }
        }

        public string ContentType
        {
            set
            {
                contentType = value;
            }
        }

        async public Task WriteAsync(string content, Encoding encoding)
        {

            writtenContent = content;
            writtenEncoding = encoding;
            await Task.Yield();
        }
    }
    public class NetmockeryTestCase
    {
        public string Name;
        public string RequestPath;
        public string RequestBody;
        public string ExpectedResponseBody;        

        async public Task<NetmockeryTestCaseResult> ExecuteAsync(EndpointCollection endpointCollection, bool handleErrors=true)
        {
            Debug.Assert(endpointCollection != null);

            var retval = new NetmockeryTestCaseResult { TestCase = this };
            try
            {
                var endpoint = endpointCollection.Resolve(RequestPath);
                if (endpoint == null)
                {
                    retval.Message = "No endpoint matches request path";
                    retval.Error = true;
                }
                else
                {
                    bool singleMatch;
                    var matcher_and_creator = endpoint.Resolve(new PathString(RequestPath), RequestBody, null, out singleMatch);
                    if (matcher_and_creator != null)
                    {
                        var responseCreator = matcher_and_creator.Item2;
                        var response = new TestCaseHttpResponse();
                        var responseBodyBytes = await responseCreator.CreateResponseAsync(new TestCaseHttpRequest(RequestPath), Encoding.UTF8.GetBytes(RequestBody), response, endpoint.Directory);
                        var responseBody = Encoding.UTF8.GetString(responseBodyBytes);
                        retval.OK = responseBody == ExpectedResponseBody;
                        retval.Error = !retval.OK;
                    }
                    else
                    {
                        retval.Message = "Endpoint has no match for request";
                        retval.Error = true;
                    }
                }
            }
            catch (Exception exception)
            {
                if (!handleErrors) throw;
                retval.Exception = exception;
                retval.Error = true;
            }
            return retval;
        }
    }

    public class NetmockeryTestCaseResult
    {
        public bool OK;
        public bool Error;
        public string Message;
        public Exception Exception;
        public NetmockeryTestCase TestCase;
    }
}
