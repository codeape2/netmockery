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
        private HeaderDictionary headerDictionary = new HeaderDictionary();

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

        async public Task<NetmockeryTestCaseResult> ExecuteAsync(EndpointCollection endpointCollection, bool handleErrors=true)
        {
            Debug.Assert(endpointCollection != null);

            var retval = new NetmockeryTestCaseResult { TestCase = this };
            try
            {
                var endpoint = endpointCollection.Resolve(RequestPath);
                if (endpoint == null)
                {
                    retval.SetFailure("No endpoint matches request path");
                }
                else
                {
                    bool singleMatch;
                    var matcher_and_creator = endpoint.Resolve(new PathString(RequestPath), new QueryString(QueryString), RequestBody, null, out singleMatch);
                    if (matcher_and_creator != null)
                    {
                        var responseCreator = matcher_and_creator.Item2;
                        string responseBody = null;
                        if (! HasExpectations)
                        {
                            retval.SetFailure("Test case has no expectations");
                        }
                        else
                        {
                            if (NeedsResponseBody)
                            {
                                var responseBodyBytes = await responseCreator.CreateResponseAsync(new TestCaseHttpRequest(RequestPath), Encoding.UTF8.GetBytes(RequestBody), new TestCaseHttpResponse(), endpoint.Directory);
                                responseBody = Encoding.UTF8.GetString(responseBodyBytes);
                            }
                            string message;
                            if (Evaluate(matcher_and_creator.Item1.ToString(), matcher_and_creator.Item2.ToString(), responseBody, out message))
                            {
                                retval.SetSuccess();
                            } else
                            {
                                retval.SetFailure(message);
                            }
                        }
                    }
                    else
                    {
                        retval.SetFailure("Endpoint has not match for request");
                    }
                }
            }
            catch (Exception exception)
            {
                if (!handleErrors) throw;
                retval.SetException(exception);
            }
            return retval;
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

        public void SetSuccess()
        {
            _ok = true;
        }

        public void SetFailure(string message)
        {
            _ok = false;
            _message = message;
        }

        public void SetException(Exception e)
        {
            Debug.Assert(e != null);
            SetFailure("Exception");
            _exception = e;
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
