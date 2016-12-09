using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net.Http;

namespace netmockery
{
    public class TestCaseHttpRequest : IHttpRequestWrapper
    {
        private string path;
        private string querystring;
        private HeaderDictionary headerDictionary = new HeaderDictionary();

        public TestCaseHttpRequest(string path, string querystring)
        {
            this.path = path;
            this.querystring = querystring;
        }

        public IHeaderDictionary Headers => headerDictionary;

        public PathString Path => new PathString(path);

        public QueryString QueryString => new QueryString(querystring);
    }

    public class TestCaseHttpResponse : IHttpResponseWrapper
    {
        MemoryStream memoryStream = new MemoryStream();
        string writtenContent;
        Encoding writtenEncoding;
        string contentType;
        bool writeAsyncCalled = false;

        public Stream Body => memoryStream;

        public string ContentType
        {
            set
            {
                contentType = value;
            }
        }

        async public Task WriteAsync(string content, Encoding encoding)
        {
            Debug.Assert(writeAsyncCalled == false);
            writtenContent = content;
            writtenEncoding = encoding;
            writeAsyncCalled = true;

            await Task.Yield();
        }

        public string GetWrittenResponseAsString()
        {
            if (! writeAsyncCalled)
            {
                throw new InvalidOperationException("This is a binary response");
            }

            return writtenContent;
        }
    }

    public class NetmockeryTestCase
    {
        public string Name;
        public string RequestPath;
        public string QueryString;
        public string RequestBody;

        public string ExpectedRequestMatcher;
        public string ExpectedResponseCreator;

        public string ExpectedResponseBody;

        public bool NeedsResponseBody
        {
            get
            {
                return (new[] { ExpectedResponseBody }).Any(val => val != null);
            }
        }

        public bool HasExpectations
        {
            get
            {
                return (new[] { ExpectedResponseBody, ExpectedRequestMatcher, ExpectedResponseCreator }).Any(val => val != null);
            }
        }


        public bool Evaluate(string requestMatcher, string responseCreator, string responseBody, out string message)
        {
            Debug.Assert(responseBody != null || !NeedsResponseBody);
            Debug.Assert(requestMatcher != null);
            Debug.Assert(responseCreator != null);
            message = null;

            if (ExpectedRequestMatcher != null && ExpectedRequestMatcher != requestMatcher)
            {
                message = $"Expected request matcher: {ExpectedRequestMatcher}\nActual: {requestMatcher}";
                return false;
            }

            if (ExpectedResponseCreator != null && ExpectedResponseCreator != responseCreator)
            {
                message = $"Expected response creator: {ExpectedResponseCreator}\nActual: {responseCreator}";
                return false;
            }

            if (ExpectedResponseBody != null && ExpectedResponseBody != responseBody)
            {
                message = $"Expected response body:\n{ExpectedResponseBody}\n\nActual response body:\n{responseBody}";
                return false;
            }

            Debug.Assert(message == null);
            return true;
        }

        async public Task<NetmockeryTestCaseResult> ExecuteAgainstHttpClientAsync(HttpClient client, string requestUrl)
        {
            var retval = new NetmockeryTestCaseResult { TestCase = this };
            if (QueryString != null)
            {
                requestUrl += QueryString;
            }
            HttpResponseMessage responseMessage;
            if (RequestBody != null)
            {
                responseMessage = await client.PostAsync(requestUrl, new ByteArrayContent(Encoding.UTF8.GetBytes(RequestBody)));
            }
            else
            {
                responseMessage = await client.GetAsync(requestUrl);
            }
            var body = await responseMessage.Content.ReadAsStringAsync();

            string message;
            var requestMatcher = "";
            if (responseMessage.Headers.Contains("X-Netmockery-RequestMatcher"))
            {
                requestMatcher = responseMessage.Headers.GetValues("X-Netmockery-RequestMatcher").ElementAt(0);
            }
            var responseCreator = "";
            if (responseMessage.Headers.Contains("X-Netmockery-ResponseCreator"))
            {
                responseCreator = responseMessage.Headers.GetValues("X-Netmockery-ResponseCreator").ElementAt(0);
            }
            if (Evaluate(requestMatcher, responseCreator, body, out message))
            {
                retval.SetSuccess();
            }
            else
            {
                retval.SetFailure(message);
            }
            return retval;

        }

        async public Task<NetmockeryTestCaseResult> ExecuteAgainstUrlAsync(string url)
        {
            return await ExecuteAgainstHttpClientAsync(new HttpClient(), url + RequestPath);
        }

        private const string ERROR_NOMATCHING_ENDPOINT = "No endpoint matches request path";
        private const string ERROR_ENDPOINT_HAS_NO_MATCH = "Endpoint has no match for request";


        async public Task<NetmockeryTestCaseResult> ExecuteAsync(EndpointCollection endpointCollection, bool handleErrors=true)
        {
            Debug.Assert(endpointCollection != null);

            var testResult = new NetmockeryTestCaseResult { TestCase = this };
            try
            {
                var endpoint = endpointCollection.Resolve(RequestPath);
                if (endpoint == null)
                {
                    return testResult.SetFailure(ERROR_NOMATCHING_ENDPOINT);
                }

                bool singleMatch;
                var matcher_and_creator = endpoint.Resolve(new PathString(RequestPath), new QueryString(QueryString), RequestBody ?? "", null, out singleMatch);
                if (matcher_and_creator == null)
                {
                    return testResult.SetFailure(ERROR_ENDPOINT_HAS_NO_MATCH);
                }
                if (!HasExpectations)
                {
                    return testResult.SetFailure("Test case has no expectations");
                }

                var responseCreator = matcher_and_creator.Item2;
                string responseBody = null;
                if (NeedsResponseBody)
                {
                    var httpResponse = new TestCaseHttpResponse();
                    var responseBodyBytes = await responseCreator.CreateResponseAsync(
                        new TestCaseHttpRequest(RequestPath, QueryString), 
                        Encoding.UTF8.GetBytes(RequestBody ?? ""), 
                        httpResponse, 
                        endpoint.Directory
                    );
                    responseBody = httpResponse.GetWrittenResponseAsString();
                }
                string message;
                if (Evaluate(matcher_and_creator.Item1.ToString(), matcher_and_creator.Item2.ToString(), responseBody, out message))
                {
                    return testResult.SetSuccess();
                }
                else
                {
                    return testResult.SetFailure(message);
                }
            }
            catch (Exception exception)
            {
                if (!handleErrors) throw;
                return testResult.SetException(exception);
            }
        }

        async public Task<Tuple<string, string>> GetResponseAsync(EndpointCollection endpointCollection)
        {
            var endpoint = endpointCollection.Resolve(RequestPath);
            if (endpoint == null)
            {
                return Tuple.Create((string)null, ERROR_NOMATCHING_ENDPOINT);
            }
            bool singleMatch;
            var matcher_and_creator = endpoint.Resolve(new PathString(RequestPath), new QueryString(QueryString), RequestBody, null, out singleMatch);
            if (matcher_and_creator != null)
            {
                var responseCreator = matcher_and_creator.Item2;
                var responseBodyBytes = await responseCreator.CreateResponseAsync(new TestCaseHttpRequest(RequestPath, QueryString), Encoding.UTF8.GetBytes(RequestBody ?? ""), new TestCaseHttpResponse(), endpoint.Directory);
                return Tuple.Create(Encoding.UTF8.GetString(responseBodyBytes), (string)null);
            }
            else
            {
                return Tuple.Create((string)null, ERROR_ENDPOINT_HAS_NO_MATCH);
            }

        }
    }

    public class NetmockeryTestCaseResult
    {
        private bool _ok;
        private Exception _exception;
        private string _message;

        public bool OK => _ok;
        public bool Error => !_ok;
        public Exception Exception => _exception;
        public string Message => _message;
        public NetmockeryTestCase TestCase;

        public NetmockeryTestCaseResult SetSuccess()
        {
            _ok = true;
            return this;
        }

        public NetmockeryTestCaseResult SetFailure(string message)
        {
            _ok = false;
            _message = message;
            return this;
        }

        public NetmockeryTestCaseResult SetException(Exception e)
        {
            Debug.Assert(e != null);
            SetFailure("Exception");
            _exception = e;
            return this;
        }

        private static string indent(string s)
        {
            Debug.Assert(s != null);
            return "    " + s.Replace(Environment.NewLine, Environment.NewLine + "    ");
        }

        public string ResultAsString
        {
            get
            {
                var shortstatus = "";
                if (OK)
                {
                    shortstatus = "OK";
                }
                if (Error)
                {
                    shortstatus = "Fail";
                }
                if (Exception != null)
                {
                    shortstatus = "Error";
                }

                if (Error)
                {
                    var retval = $"{shortstatus}\n{indent(Message ?? "")}";
                    if (Exception != null)
                    {
                        retval += "\n" + indent(Exception.ToString());
                    }
                    return retval;
                }
                else
                {
                    return shortstatus;
                }
            }
        }
    }
}
