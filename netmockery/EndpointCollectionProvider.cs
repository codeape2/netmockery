using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace netmockery
{
    public class EndpointCollectionProvider
    {
        private List<DateTime> reloadTimestamps = new List<DateTime>();
        private EndpointCollection endpointCollection;
        private string directory;
        private bool canReload;

        public EndpointCollectionProvider(string directory)
        {
            Debug.Assert(directory != null);
            Debug.Assert(Directory.Exists(directory));

            this.directory = directory;
            canReload = true;
            Reload();
        }

        public EndpointCollectionProvider(EndpointCollection endpointCollection)
        {
            reloadTimestamps.Add(DateTime.Now);
            canReload = false;
            this.endpointCollection = endpointCollection;
        }

        public void Reload()
        {
            if (! canReload)
            {
                throw new InvalidOperationException();
            }
            endpointCollection = EndpointCollectionReader.ReadFromDirectory(directory);
            reloadTimestamps.Add(DateTime.Now);
        }

        public EndpointCollection EndpointCollection => endpointCollection;

        public DateTime[] ReloadTimestamps => reloadTimestamps.ToArray();
    }
}
