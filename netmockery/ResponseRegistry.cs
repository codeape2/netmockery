using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace netmockery
{
    public class ResponseRegistryItem
    {
        public int Id;
        public DateTime Timestamp;
        public Endpoint Endpoint;
        public RequestMatcher RequestMatcher;
        public ResponseCreator ResponseCreator;
        public string RequestPath;
        public string QueryString;
        public string RequestBody;
        public string ResponseBody;
        public string Error;
        public bool SingleMatch;

        public void WriteToConsole()
        {
            Console.WriteLine($"{Timestamp.ToString("HH:mm:ss.fff")} {RequestPath} {Endpoint?.Name} {Error}");
            if (RequestMatcher != null)
            {
                Console.WriteLine("    " + RequestMatcher.ToString());
            }
            if (ResponseCreator != null)
            {
                Console.WriteLine("    " + ResponseCreator.ToString());
            }
        }
    }

    public class FailedRequestItem
    {
        public DateTime Timestamp;
    }
    
    public class ResponseRegistry
    {
        private int _nextId;
        private Queue<ResponseRegistryItem> _items = new Queue<ResponseRegistryItem>();

        public int Capacity { get; set; } = 1000;

        public IEnumerable<ResponseRegistryItem> ForEndpoint(string endpointName)
        {
            return _items.Where(item => item?.Endpoint?.Name == endpointName);
        }

        public ResponseRegistryItem Get(int id)
        {
            return _items.Where(item => item.Id == id).Single();
        }

        public IEnumerable<ResponseRegistryItem> Responses => _items.Reverse<ResponseRegistryItem>();

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Add(ResponseRegistryItem responseRegistryItem)
        {
            Debug.Assert(responseRegistryItem.Id == 0);
            responseRegistryItem.Id = ++_nextId;
            if (_items.Count >= Capacity)
            {
                _items.Dequeue();
            }
            _items.Enqueue(responseRegistryItem);
        }
    }
}
