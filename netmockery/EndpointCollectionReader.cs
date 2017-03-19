using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;

namespace netmockery
{
    static public class EndpointCollectionReader
    {
        static public EndpointCollection ReadFromDirectory(string directoryName)
        {
            var retval = new EndpointCollection { SourceDirectory = directoryName };
            var globalDefaultsFile = Path.Combine(directoryName, "defaults.json");

            var globalDefaults = 
                File.Exists(globalDefaultsFile) 
                ?
                JsonConvert.DeserializeObject<JSONDefaults>(File.ReadAllText(globalDefaultsFile)) 
                : 
                null;

            foreach (var subdirectory in Directory.GetDirectories(directoryName))
            {
                var endpointFile = Path.Combine(subdirectory, "endpoint.json");                
                if (File.Exists(endpointFile))
                {
                    retval.Add(JSONReader.ReadEndpoint(File.ReadAllText(endpointFile), subdirectory, globalDefaults));
                }                
            }
            return retval;
        }
    }
}
