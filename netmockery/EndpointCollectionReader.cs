using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;

namespace netmockery
{
    static public class EndpointCollectionReader
    {
        static public EndpointCollection ReadFromDirectory(string directoryName)
        {
            var retval = new EndpointCollection { SourceDirectory = directoryName };
            foreach (var subdirectory in Directory.GetDirectories(directoryName))
            {
                var endpointFile = Path.Combine(subdirectory, "endpoint.json");
                if (File.Exists(endpointFile))
                {
                    retval.Add(JSONReader.ReadEndpoint(File.ReadAllText(endpointFile), subdirectory));
                }                
            }
            return retval;
        }
    }
}
