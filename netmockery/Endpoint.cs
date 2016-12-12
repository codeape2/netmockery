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
        private bool _anyHasBeenAdded = false;        

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

        public string Directory { get; set; }

        public string Name => _name;
        public string PathRegex => _pathregex;

        public IEnumerable<Tuple<RequestMatcher, ResponseCreator>> Responses => _responses.AsReadOnly();

        public bool Matches(string path)
        {
            return Regex.IsMatch(path, _pathregex);
        }

        public void Add(RequestMatcher requestMatcher, ResponseCreator responseCreator)
        {
            Debug.Assert(requestMatcher != null);
            Debug.Assert(responseCreator != null);
            Debug.Assert(requestMatcher.Index == -1);

            if (_anyHasBeenAdded)
            {
                throw new ArgumentException("The endpoint contains a response matching any request, you cannot add more responses");
            }

            requestMatcher.Index = _responses.Count;
            _responses.Add(Tuple.Create(requestMatcher, responseCreator));
            if (requestMatcher is AnyMatcher)
            {
                _anyHasBeenAdded = true;
            }
        }

        public ResolutionResult Resolve(PathString path, QueryString queryString, string body, IHeaderDictionary headers)
        {
            var candidates = (from t in _responses where t.Item1.Matches(path, queryString, body, headers) select t).Take(2);
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
