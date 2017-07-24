﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;


namespace netmockery
{
    public static class JSONReader
    {
        public static Endpoint ReadEndpoint(string jsonString, string rootDir, JSONDefaults globalDefaults)
        {
            return JsonConvert.DeserializeObject<JSONEndpoint>(jsonString).CreateEndpoint(rootDir, globalDefaults);
        }
    }

    public class JSONParam
    {
        public string name;
        public string @default;
        public string description;

        public JSONParam Validated()
        {
            if (name == null)
            {
                throw new ArgumentException($"Parameter missing name");
            }

            if (! Regex.IsMatch(name, "^[a-zA-Z_]+$"))
            {
                throw new ArgumentException($"Invalid parameter name: '{name}'");
            }

            if (@default == null)
            {
                throw new ArgumentException($"Missing default value for parameter '{name}'");
            }

            if (description == null)
            {
                throw new ArgumentException($"Missing description for parameter '{name}'");
            }

            return this;
        }
    }

    public class JSONTest
    {
        public string name;
        public string method;
        public string requestpath;
        public string querystring;
        public string requestbody;

        public string expectedrequestmatcher;
        public string expectedresponsecreator;
        public string expectedresponsebody;
        public string expectedcontenttype;
        public string expectedcharset;
        public int? expectedstatuscode;

        public JSONTest Validated()
        {
            if (requestpath == null)
            {
                throw new ArgumentNullException(nameof(requestpath));
            }
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            return this;
        }

        public NetmockeryTestCase CreateTestCase(string directory)
        {
            return new NetmockeryTestCase {
                Name = name,
                Method = method ?? "GET",
                RequestPath = requestpath,
                QueryString = querystring,
                RequestBody =
                    requestbody != null && requestbody.StartsWith("file:")
                    ?
                    File.ReadAllText(Path.Combine(directory, requestbody.Substring(5)))
                    :
                    requestbody,

                ExpectedRequestMatcher = expectedrequestmatcher,
                ExpectedResponseCreator = expectedresponsecreator,

                ExpectedContentType = expectedcontenttype,
                ExpectedCharSet = expectedcharset,
                ExpectedStatusCode = expectedstatuscode,
                ExpectedResponseBody = 
                    expectedresponsebody != null && expectedresponsebody.StartsWith("file:")
                    ? 
                    File.ReadAllText(Path.Combine(directory, expectedresponsebody.Substring(5)))
                    : 
                    expectedresponsebody
            };
        }
    }

    public class JSONResponse
    {
        public JSONRequestMatcher match;

        public string literal;

        public string file;
        public string script;

        public string forward;
        public string proxy;
        public string strippath;

        //TODO: contenttype should be renamed to mediatype
        public string contenttype;
        public string charset;
        public string statuscode;
        public JSONReplacement[] replacements;
        public string delay;

        public JSONResponse Validated()
        {
            if (match == null)
            {
                throw new ArgumentException("match must be specified");
            }
            var mutuallyExclusive = new[] { literal, file, script, forward };
            var mutExWithValues = from value in mutuallyExclusive where value != null select value;
            if (mutExWithValues.Count() != 1)
            {
                throw new ArgumentException("Exactly one of file, script or forward must be set");
            }
            //TODO: Implement related validation
            //TODO: Implement set if main not set validation (i.e. proxy set but not forward)
            
            //TODO: Implement invalid for type validation (i.e. contenttype if a forward response creator)
            // but remember this is problematic in the case of global defaults
            // unclean solution is to apply defaults after validation
            // clean solution is to apply defaults only based on rules, i.e. for forward response creator, do not apply defaults
            return this;
        }

        public ResponseCreator CreateResponseCreator(Endpoint endpoint)
        {
            ResponseCreator responseCreator = null;
            if (literal != null)
            {
                responseCreator = new LiteralResponse(literal, endpoint);
            }
            else if (file != null)
            {
                responseCreator = new FileResponse(file, endpoint); // Path.Combine(rootDir, file)); ;
            }
            else if (script != null)
            {
                responseCreator = new FileDynamicResponseCreator(script, endpoint);
            }
            else if (forward != null)
            {
                responseCreator = new ForwardResponseCreator(forward, endpoint) { ProxyUrl = proxy, StripPath = strippath };
            }
            else
            {
                throw new NotImplementedException();
            }

            responseCreator.SetDelayFromConfigValue(delay);

            if (responseCreator is SimpleResponseCreator simpleResponseCreator)
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

                Debug.Assert(simpleResponseCreator.Encoding == Encoding.UTF8);
                if (charset != null)
                {
                    simpleResponseCreator.Encoding = Encoding.GetEncoding(charset);
                }
                simpleResponseCreator.SetStatusCodeFromString(statuscode);
            }

            return responseCreator;
        }

    }

    public class JSONRequestMatcher
    {
        public string methods;
        public string xpath;
        public string regex;
        public JSONXPathNamespace[] namespaces;

        public RequestMatcher CreateRequestMatcher()
        {
            RequestMatcher retval;
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
                retval = xpathMatcher;
            }
            else if (regex != null)
            {
                retval = new RegexMatcher(regex);
            }
            else
            {
                retval = new AnyMatcher();
            }
            if (methods != null)
            {
                retval.SetMatchingHttpMethods(methods);
            }
            return retval;
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

    public class JSONEndpoint
    {
        public string name;
        public string pathregex;
        public JSONResponse[] responses;


        public Endpoint CreateEndpoint(string rootDir, JSONDefaults globalDefaults)
        {
            var endpoint = new Endpoint(name, pathregex)
            {
                Directory = rootDir
            };

            var endpointDefaultsFile = Path.Combine(rootDir, "defaults.json");
            var endpointDefaults =
                File.Exists(endpointDefaultsFile)
                ?
                JsonConvert.DeserializeObject<JSONDefaults>(File.ReadAllText(endpointDefaultsFile))
                :
                null;

            foreach (var jsonResponse in responses)
            {
                if (endpointDefaults != null)
                {
                    applyDefaults(endpointDefaults, jsonResponse);
                }

                if (globalDefaults != null)
                {
                    applyDefaults(globalDefaults, jsonResponse);
                }
                var validatedJsonResponse = jsonResponse.Validated();
                endpoint.Add(validatedJsonResponse.match.CreateRequestMatcher(), validatedJsonResponse.CreateResponseCreator(endpoint));
            }

            var paramsFile = Path.Combine(rootDir, "params.json");
            if (File.Exists(paramsFile))
            {
                var jsonParams = from item in JsonConvert.DeserializeObject<JSONParam[]>(File.ReadAllText(paramsFile)) select item.Validated();
                foreach (var jsonParam in jsonParams)
                {
                    endpoint.AddParameter(new EndpointParameter
                    {
                        Name = jsonParam.name,
                        DefaultValue = jsonParam.@default,
                        Value = jsonParam.@default,
                        Description = jsonParam.description
                    });
                }
            }

            return endpoint;
        }

        private void applyDefaults(JSONDefaults defaults, JSONResponse jsonResponse)
        {
            Debug.Assert(defaults != null);
            Debug.Assert(jsonResponse != null);

            if (defaults.charset != null && jsonResponse.charset == null)
            {
                jsonResponse.charset = defaults.charset;
            }

            if (defaults.contenttype != null && jsonResponse.contenttype == null)
            {
                jsonResponse.contenttype = defaults.contenttype;
            }
        }
    }

    public class JSONDefaults
    {
        public string contenttype;
        public string charset;
    }
}
