using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Net;
using netmockery.globals;

namespace netmockery
{    
    public interface IHttpResponseWrapper
    {
        string ContentType { set; }
        Stream Body { get; }
        HttpStatusCode HttpStatusCode { set; }

        Task WriteAsync(string content, Encoding encoding);
    }

    public interface IHttpRequestWrapper
    {
        PathString Path { get; }
        QueryString QueryString { get; }
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
        public IHeaderDictionary Headers => httpRequest.Headers;
        public PathString Path => httpRequest.Path;
        public QueryString QueryString => httpRequest.QueryString;
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

        public HttpStatusCode HttpStatusCode
        {
            set
            {
                httpResponse.StatusCode = (int)value;
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

    public class BodyReplacement
    {
        public string SearchTerm;
        public string ReplacementTerm;
    }

    public abstract class ResponseCreator
    {
        private Endpoint _endpoint;
        private string _delay;

        public int Index = -1;

        public ResponseCreator(Endpoint endpoint)
        {
            Debug.Assert(endpoint != null);
            _endpoint = endpoint;
        }

        public Endpoint Endpoint => _endpoint;

        public int Delay
        {
            get
            {
                if (_delay == null)
                {
                    return 0;
                }
                else
                {
                    return int.Parse(ReplaceParameterReference(_delay));
                }
            }
        }

        public void SetDelayFromConfigValue(string value)
        {
            _delay = value;
        }

        public abstract Task<byte[]> CreateResponseAsync(IHttpRequestWrapper request, byte[] requestBody, IHttpResponseWrapper response, Endpoint endpoint);

        public bool IsParameterReference(string value)
        {
            return value != null && value.StartsWith("$");
        }

        public string LookupParameter(string value)
        {
            Debug.Assert(IsParameterReference(value));

            return _endpoint.GetParameter(value.Substring(1)).Value;
        }

        public string ReplaceParameterReference(string value)
        {
            if (IsParameterReference(value))
            {
                return LookupParameter(value);
            }
            else
            {
                return value;
            }
        }

    }

    public abstract class SimpleResponseCreator : ResponseCreator
    {
        private string _contentType;
        private string _statuscode;

        public SimpleResponseCreator(Endpoint endpoint) : base(endpoint)
        {

        }

        public void SetStatusCodeFromString(string value)
        {
            _statuscode = value;
        }

        public override async Task<byte[]> CreateResponseAsync(IHttpRequestWrapper request, byte[] requestBody, IHttpResponseWrapper response, Endpoint endpoint)
        {
            var requestInfo = new RequestInfo
            {
                RequestPath = request.Path.ToString(),
                QueryString = request.QueryString.ToString(),
                RequestBody = Encoding.UTF8.GetString(requestBody),
                Headers = request.Headers,
                EndpointDirectory = endpoint.Directory
            };
            var responseBody = await GetBodyAndExecuteReplacementsAsync(requestInfo);

            SetContentType(requestInfo, response);
            SetStatusCode(requestInfo, response);

            await response.WriteAsync(responseBody, Encoding);
            return Encoding.GetBytes(responseBody);
        }

        protected virtual void SetContentType(RequestInfo requestInfo, IHttpResponseWrapper response)
        {
            // extension point, override to add logic in inheritors.
            // currently used by DynamicResponseCreator in order to let script code override content type

            if (ContentType != null)
            {
                var contenttype = ContentType;
                contenttype += $"; charset={Encoding.WebName}";
                response.ContentType = contenttype;
            }
        }

        protected virtual void SetStatusCode(RequestInfo requestInfo, IHttpResponseWrapper response)
        {
            // extension point, override to add logic in inheritors.
            // currently used by DynamicResponseCreator in order to let script code override status code

            response.HttpStatusCode = (HttpStatusCode)StatusCode;
        }

        public string ContentType {
            get { return ReplaceParameterReference(_contentType); }
            set { _contentType = value; }
        }
        public abstract Task<string> GetBodyAsync(RequestInfo requestInfo);
        public Encoding Encoding { get; set; } = Encoding.UTF8;


        public int StatusCode {
            get
            {
                if (_statuscode == null)
                {
                    return 200;
                }
                else
                {
                    return int.Parse(ReplaceParameterReference(_statuscode));
                }
            }
        }

        public BodyReplacement[] Replacements = new BodyReplacement[0];

        public async Task<string> GetBodyAndExecuteReplacementsAsync(RequestInfo requestInfo)
        {
            var retval = await GetBodyAsync(requestInfo);

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

        public string Body => ReplaceParameterReference(_body);

        public LiteralResponse(string body, Endpoint endpoint) : base(endpoint)
        {
            _body = body;
        }

        public override Task<string> GetBodyAsync(RequestInfo requestInfo) => Task.FromResult(Body);

        public override string ToString() => $"Literal string: {Body}";
    }

    public class FileResponse : SimpleResponseCreator, IResponseCreatorWithFilename
    {
        private string _filename;

        public FileResponse(string filename, Endpoint endpoint) : base(endpoint)
        {
            Debug.Assert(filename != null);
            _filename = filename;
        }

        public string Filename => Path.Combine(Endpoint.Directory, ReplaceParameterReference(_filename));

        public override async Task<string> GetBodyAsync(RequestInfo requestInfo)
        {
            using (var reader = File.OpenText(Filename))
            {
                return await reader.ReadToEndAsync();
            }
        }

        public override string ToString() => $"File {Path.GetFileName(Filename)}";
    }

}
