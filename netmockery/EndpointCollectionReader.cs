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
                retval.Add(JSONReader.ReadEndpoint(File.ReadAllText(Path.Combine(subdirectory, "endpoint.json")), subdirectory));
            }
            return retval;
        }
    }
}
