using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace netmockery
{
    public class ResolutionResult
    {
        public RequestMatcher RequestMatcher;
        public ResponseCreator ResponseCreator;
        public bool SingleMatch;
        public int MatchIndex => RequestMatcher.Index;
    }

    public class Endpoint
    {
        private string _name;
        private string _pathregex;
        private List<Tuple<RequestMatcher, ResponseCreator>> _responses = new List<Tuple<RequestMatcher, ResponseCreator>>();
        private List<EndpointParameter> _parameters = new List<EndpointParameter>();
        private Dictionary<string, object> _scriptObjects = new Dictionary<string, object>();
        private bool _ruleThatCatchesEveryThingHasBeenAdded = false;        

        public Endpoint(string name, string pathregex)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (pathregex == null)
            {
                throw new ArgumentNullException(nameof(pathregex));
            }

            _name = name;
            _pathregex = pathregex;
        }

        //public IEnumerable<EndpointParameter> Parameters => _parameters;

        public string Directory { get; set; }
        public bool RecordRequests { get; set; }

        public string Name => _name;
        public string PathRegex => _pathregex;

        public IEnumerable<Tuple<RequestMatcher, ResponseCreator>> Responses => _responses.AsReadOnly();

        public bool Matches(string path)
        {
            return Regex.IsMatch(path, _pathregex);
        }

        public void AddParameter(EndpointParameter parameter)
        {
            Debug.Assert(parameter != null);

            if (_parameters.Select(p => p.Name).Contains(parameter.Name))
            {
                throw new ArgumentException($"Duplicate parameter name '{parameter.Name}'");
            }

            _parameters.Add(parameter);
        }

        public EndpointParameter GetParameter(string name)
        {
            Debug.Assert(_parameters != null);
            var retval = _parameters.SingleOrDefault(p => p.Name == name);
            if (retval == null)
            {
                throw new ArgumentException($"Endpoint parameter '{name}' not found");
            }
            return retval;
        }

        public EndpointParameter GetParameter(int parameterIndex)
        {
            Debug.Assert(_parameters != null);
            if (parameterIndex < ParameterCount && parameterIndex >= 0)
            {
                return _parameters.ElementAt(parameterIndex);
            }
            else
            {
                throw new ArgumentException($"Invalid parameter index {parameterIndex}");
            }
        }

        public bool HasAdjustedParams => _parameters.Any(p => !p.ValueIsDefault);

        public int ParameterCount => _parameters.Count;

        public object GetScriptObject(string name) => _scriptObjects[name];

        public bool HasScriptObject(string name) => _scriptObjects.ContainsKey(name);

        public void SetScriptObject(string name, object value) => _scriptObjects[name] = value;

        public void Add(RequestMatcher requestMatcher, ResponseCreator responseCreator)
        {
            Debug.Assert(requestMatcher != null);
            Debug.Assert(responseCreator != null);
            Debug.Assert(requestMatcher.Index == -1);

            if (_ruleThatCatchesEveryThingHasBeenAdded)
            {
                throw new ArgumentException("The endpoint contains a response matching any request/method, you cannot add more responses");
            }

            requestMatcher.Index = _responses.Count;
            _responses.Add(Tuple.Create(requestMatcher, responseCreator));
            if (requestMatcher is AnyMatcher && requestMatcher.MatchesAnyHttpMethod)
            {
                _ruleThatCatchesEveryThingHasBeenAdded = true;
            }
        }

        public ResolutionResult Resolve(string httpMethod, PathString path, QueryString queryString, string body, IHeaderDictionary headers)
        {
            var candidates = (
                from t in _responses
                where t.Item1.MatchesHttpMethod(httpMethod) && t.Item1.Matches(path, queryString, body, headers)
                select t
                ).Take(2);

            if (! candidates.Any())
            {
                return null;
            }
            var matcherAndCreator = candidates.First();
            return new ResolutionResult
            {
                RequestMatcher = matcherAndCreator.Item1,
                ResponseCreator = matcherAndCreator.Item2,
                SingleMatch = candidates.Count() == 1,
            };
        }
    }
}
