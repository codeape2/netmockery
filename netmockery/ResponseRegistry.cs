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
        public string Method;
        public string RequestPath;
        public string QueryString;
        public string RequestBody;
        public string ResponseBody;
        public string Error;
        public bool SingleMatch;

        public bool HasBeenAddedToRegistry => Id != 0;

        public void WriteIncomingInfoToConsole()
        {
            Debug.Assert(HasBeenAddedToRegistry);
            Console.WriteLine($"[{Id}] {Timestamp.ToString("HH:mm:ss.fff")} {Method} {RequestPath}");
        }

        public void WriteResolvedInfoToConsole()
        {
            Debug.Assert(HasBeenAddedToRegistry);
            if (Endpoint != null)
            {
                Console.WriteLine($"[{Id}] Endpoint: {Endpoint.Name}");
            }
            if (RequestMatcher != null)
            {
                Console.WriteLine($"[{Id}] Request matcher: {RequestMatcher}");
            }
            if (ResponseCreator != null)
            {
                Console.WriteLine($"[{Id}] Response creator: {ResponseCreator}");
            }
            if (Error != null)
            {
                Console.WriteLine($"[{Id}] Error: {Error}");
            }
        }
    }

   
    public class ResponseRegistry
    {
        private int _nextId;
        private Queue<ResponseRegistryItem> _items = new Queue<ResponseRegistryItem>();
        private object _lock = new object();

        public int Capacity { get; set; } = 1000;

        public IEnumerable<ResponseRegistryItem> ForEndpoint(string endpointName)
        {
            return Responses.Where(item => item?.Endpoint?.Name == endpointName);
        }

        public ResponseRegistryItem Get(int id)
        {
            return _items.Where(item => item.Id == id).Single();
        }

        public void Clear()
        {
            _items.Clear();
        }

        public IEnumerable<ResponseRegistryItem> Responses => _items.Reverse<ResponseRegistryItem>();

        public ResponseRegistryItem Add(ResponseRegistryItem responseRegistryItem)
        {
            Debug.Assert(responseRegistryItem.Id == 0);
            lock (_lock)
            {
                responseRegistryItem.Id = ++_nextId;
                if (_items.Count >= Capacity)
                {
                    _items.Dequeue();
                }
                _items.Enqueue(responseRegistryItem);
            }
            return responseRegistryItem;
        }

        public void AddAndWriteIncomingInfoToConsole(ResponseRegistryItem responseRegistryItem)
        {
            Add(responseRegistryItem);
            responseRegistryItem.WriteIncomingInfoToConsole();
        }
    }
}
