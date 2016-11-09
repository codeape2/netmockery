using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace netmockery
{    
    public interface IHttpResponseWrapper
    {
        string ContentType { set; }
        Stream Body { get; }

        Task WriteAsync(string content, Encoding encoding);
    }

    public interface IHttpRequestWrapper
    {
        PathString Path { get; }
        IHeaderDictionary Headers { get; }
    }

    public class HttpRequestWrapper : IHttpRequestWrapper
    {
        HttpRequest httpRequest;

        public HttpRequestWrapper(HttpRequest httpRequest)
        {
            Debug.Assert(httpRequest != null);
            this.httpRequest = httpRequest;
        }
        public IHeaderDictionary Headers
        {
            get
            {
                return httpRequest.Headers;
            }
        }

        public PathString Path
        {
            get
            {
                return httpRequest.Path;
            }
        }
    }

    public class HttpResponseWrapper : IHttpResponseWrapper
    {
        private HttpResponse httpResponse;
        public HttpResponseWrapper(HttpResponse httpResponse)
        {
            Debug.Assert(httpResponse != null);
            this.httpResponse = httpResponse;
        }
        public Stream Body
        {
            get
            {
                return httpResponse.Body;
            }
        }

        public string ContentType
        {
            set
            {
                httpResponse.ContentType = value;
            }
        }

        async public Task WriteAsync(string content, Encoding encoding)
        {
            await httpResponse.WriteAsync(content, encoding);
        }
    }

    public interface IResponseCreatorWithFilename
    {
        string Filename { get; }
    }

    public class RequestInfo
    {
        public string RequestBody;
        public string RequestPath;
        public IHeaderDictionary Headers;
        public string EndpointDirectory;
    }

    public class BodyReplacement
    {
        public string SearchTerm;
        public string ReplacementTerm;
    }

    public abstract class ResponseCreator
    {
        public int Delay { get; set; } = 0;
        public abstract Task<byte[]> CreateResponseAsync(IHttpRequestWrapper request, byte[] requestBody, IHttpResponseWrapper response, string endpointDirectory);
    }

    public abstract class SimpleResponseCreator : ResponseCreator
    {
        public override async Task<byte[]> CreateResponseAsync(IHttpRequestWrapper request, byte[] requestBody, IHttpResponseWrapper response, string endpointDirectory)
        {
            var responseBody = GetBodyAndExecuteReplacements(new RequestInfo
            {
                RequestBody = Encoding.UTF8.GetString(requestBody),
                RequestPath = request.Path.ToString(),
                Headers = request.Headers,
                EndpointDirectory = endpointDirectory
            });
            response.ContentType = ContentType;
            await response.WriteAsync(responseBody, Encoding);
            return Encoding.UTF8.GetBytes(responseBody);
        }
        public string ContentType { get; set; }
        public abstract string GetBody(RequestInfo requestInfo);
        public Encoding Encoding => Encoding.UTF8;
        
        public BodyReplacement[] Replacements = new BodyReplacement[0];

        public string GetBodyAndExecuteReplacements(RequestInfo requestInfo)
        {
            var retval = GetBody(requestInfo);

            Debug.Assert(Replacements != null);
            if (Replacements.Length > 0)
            {
                foreach (var bodyReplacement in Replacements)
                {
                    retval = Regex.Replace(retval, bodyReplacement.SearchTerm, bodyReplacement.ReplacementTerm);
                }
            }
            return retval;
        }

    }


    public class LiteralResponse : SimpleResponseCreator
    {
        private string _body;

        public string Body => _body;

        public LiteralResponse(string body)
        {
            _body = body;
        }

        public override string GetBody(RequestInfo requestInfo) => _body;

        public override string ToString() => $"Literal string: {Body}";
    }

    public class FileResponse : SimpleResponseCreator, IResponseCreatorWithFilename
    {
        private string _filename;

        public FileResponse(string filename)
        {
            _filename = filename;
        }

        public string Filename => _filename;

        public override string GetBody(RequestInfo requestInfo) => File.ReadAllText(_filename);

        public override string ToString() => $"File {Path.GetFileName(_filename)}";
    }

}
