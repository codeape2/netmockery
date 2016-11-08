using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Diagnostics;
using System.IO;

namespace netmockery
{
    public static class JSONReader
    {
        public static Endpoint ReadEndpoint(string jsonString, string rootDir)
        {
            return JsonConvert.DeserializeObject<JSONEndpoint>(jsonString).CreateEndpoint(rootDir);
        }
    }

    public class JSONTest
    {
        /*
         * Typer tester vi boer stoette:
         * - expectedendpointname
         * - expectedrequestmatcher
         */
        public string name;
        public string requestpath;
        public string requestbody;
        public string expectedresponsebody;

        public NetmockeryTestCase CreateTestCase()
        {
            if (requestpath == null)
            {
                throw new ArgumentNullException(nameof(requestpath));
            }
            return new NetmockeryTestCase { Name = name, RequestPath = requestpath, RequestBody = requestbody ?? "", ExpectedResponseBody = expectedresponsebody };
        }
    }

    public class JSONResponse
    {
        public JSONRequestMatcher match;
        public JSONResponseDefinition response;
    }

    public class JSONRequestMatcher
    {
        public string xpath;
        public string regex;
        public JSONXPathNamespace[] namespaces;

        public RequestMatcher CreateRequestMatcher()
        {
            if (xpath != null)
            {
                var xpathMatcher = new XPathMatcher(xpath);
                if (namespaces != null)
                {
                    foreach (var jsonNs in namespaces)
                    {
                        xpathMatcher.AddNamespace(jsonNs.prefix, jsonNs.ns);
                    }
                }
                return xpathMatcher;
            }
            else if (regex != null)
            {
                return new RegexMatcher(regex);
            }
            return new AnyMatcher();
        }
    }

    public class JSONXPathNamespace
    {
        public string prefix;
        public string ns;
    }

    public class JSONReplacement
    {
        public string search;
        public string replace;
    }

    public class JSONResponseDefinition
    {
        public string literal;

        public string file;
        public string script;

        public string assembly;
        public string @class;
        public string method;
        
        public string forward;
        public string proxy;
        public string strippath;

        public string contenttype;
        public JSONReplacement[] replacements;
        public int delay;

        public JSONResponseDefinition Validated()
        {
            var mutuallyExclusive = new[] { literal, file, script, assembly, forward };
            var mutExWithValues = from value in mutuallyExclusive where value != null select value;
            if (mutExWithValues.Count() != 1)
            {
                throw new ArgumentException("Exactly one of file, script, assembly or forward must be set");
            }
            //TODO: Implement related validation
            //TODO: Implement set if main not set validation (i.e. proxy set but not forward)
            //TODO: Implement invalid for type validation (i.e. contenttype if a forward response creator)
            return this;
        }

        public ResponseCreator CreateResponseCreator(string rootDir)
        {
            ResponseCreator responseCreator = null;
            if (literal != null)
            {
                responseCreator = new LiteralResponse(literal);
            }
            else if (file != null)
            {
                responseCreator = new FileResponse(Path.Combine(rootDir, file)); ;
            }
            else if (script != null)
            {
                responseCreator = new FileDynamicResponseCreator(Path.Combine(rootDir, script));
            }
            else if (assembly != null)
            {
                responseCreator = new AssemblyResponseCreator
                {
                    AssemblyFilename = Path.Combine(rootDir, assembly),
                    ClassName = @class,
                    MethodName = method,
                };
            }
            else if (forward != null)
            {
                responseCreator = new ForwardResponseCreator(forward) { ProxyUrl = proxy, StripPath = strippath };
            }
            else
            {
                throw new NotImplementedException();
            }
            responseCreator.Delay = delay;

            var simpleResponseCreator = responseCreator as SimpleResponseCreator;
            if (simpleResponseCreator != null)
            {
                simpleResponseCreator.ContentType = contenttype;
                if (replacements != null)
                {
                    simpleResponseCreator.Replacements = (
                            from jsonreplacement in replacements
                            select new BodyReplacement { SearchTerm = jsonreplacement.search, ReplacementTerm = jsonreplacement.replace }
                            ).ToArray();
                }
                else
                {
                    simpleResponseCreator.Replacements = new BodyReplacement[0];
                }
            }

            return responseCreator;
        }
    }

    public class JSONEndpoint
    {
        public string name;
        public string pathregex;
        public JSONResponse[] responses;


        public Endpoint CreateEndpoint(string rootDir)
        {
            var endpoint = new Endpoint(name, pathregex);
            foreach (var jsonResponse in responses)
            {
                endpoint.Add(jsonResponse.match.CreateRequestMatcher(), jsonResponse.response.Validated().CreateResponseCreator(rootDir));
            }
            endpoint.Directory = rootDir;
            return endpoint;
        }
    }
}
