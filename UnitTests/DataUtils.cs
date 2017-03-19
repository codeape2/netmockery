using netmockery;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UnitTests
{
    public static class DataUtils
    {
        public static JSONEndpoint CreateSimpleEndpoint(string name, string file, string pathregex = ".")
        {
            return new JSONEndpoint
            {
                name = name,
                pathregex = pathregex,
                responses = new[] {
                    new JSONResponse {
                        match = new JSONRequestMatcher(),
                        file = file,
                        contenttype = "text/plain"
                    }
                }
            };

        }

        public static JSONEndpoint CreateScriptEndpoint(string name, string scriptFile, string pathregex = ".")
        {
            return new JSONEndpoint
            {
                name = name,
                pathregex = pathregex,
                responses = new[] {
                    new JSONResponse {
                        match = new JSONRequestMatcher(),
                        script = scriptFile,
                        contenttype = "text/plain"
                    }
                }
            };
        }
    }
}
