using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;

namespace netmockery.globals
{
    public class RequestInfo
    {
        public const int DEFAULT_STATUS_CODE = -1;
        public const string DEFAULT_CONTENT_TYPE = null;

        private DateTime _now = DateTime.MinValue;

        public string RequestPath;
        public string QueryString;
        public string RequestBody;
        public int StatusCode = DEFAULT_STATUS_CODE;
        public string ContentType = DEFAULT_CONTENT_TYPE;
        public IDictionary<string, StringValues> Headers;
        public string EndpointDirectory;

        public DateTime GetNow() => _now == DateTime.MinValue ? DateTime.Now : _now;

        public void SetStaticNow(DateTime now)
        {
            _now = now;
        }
    }

}