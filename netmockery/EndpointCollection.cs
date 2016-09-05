using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace netmockery
{
    public class EndpointCollection
    {
        private List<Endpoint> _endpoints = new List<Endpoint>();
        public string SourceDirectory { get; set; }

        public void Add(Endpoint endpoint)
        {
            if (endpoint == null)
            {
                throw new ArgumentNullException(nameof(endpoint));
            }
            Debug.Assert(endpoint.Name != null);

            if ((from e in _endpoints select e.Name).Contains(endpoint.Name))
            {
                throw new ArgumentException($"Duplicate endpoint name '{endpoint.Name}'", nameof(endpoint));
            }
            _endpoints.Add(endpoint);
        }

        public IEnumerable<Endpoint> Endpoints => _endpoints.AsReadOnly();

        public Endpoint Get(string name)
        {
            return _endpoints.Single(e => e.Name == name);
        }
        public Endpoint Resolve(string path)
        {
            return (from endpoint in _endpoints where endpoint.Matches(path) select endpoint).SingleOrDefault();
        }
    }
}
