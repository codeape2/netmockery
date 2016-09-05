using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace netmockery
{
    public abstract class RequestMatcher
    {
        public int Index = -1;
        public abstract bool Matches(PathString path, string body, IHeaderDictionary headers);
    }

    public class AnyMatcher : RequestMatcher
    {
        public override bool Matches(PathString path, string body, IHeaderDictionary headers)
        {
            return true;
        }

        public override string ToString()
        {
            return "Any request";
        }
    }

    public class RegexMatcher : RequestMatcher
    {
        string _regex;

        public RegexMatcher(string regex)
        {
            _regex = regex;
        }

        public string Expression => _regex;

        public override bool Matches(PathString path, string body, IHeaderDictionary headers)
        {
            return Regex.IsMatch(body, _regex);
        }

        public override string ToString()
        {
            return $"Regex '{_regex}'";
        }
    }

    public class XPathMatcher : RequestMatcher
    {
        string _xpath;
        private List<string> _prefixes = new List<string>();
        private List<string> _namespaces = new List<string>();
        public XPathMatcher(string xpath)
        {
            Debug.Assert(xpath != null);
            _xpath = xpath;
        }

        public string XPathExpresssion => _xpath;
        public string[] Namespaces => _namespaces.ToArray();
        public string[] Prefixes => _prefixes.ToArray();

        public void AddNamespace(string prefix, string ns)
        {
            _prefixes.Add(prefix);
            _namespaces.Add(ns);
        }

        public override bool Matches(PathString path, string body, IHeaderDictionary headers)
        {
            var reader = XmlReader.Create(new StringReader(body));
            var root = XElement.Load(reader);
            var nametable = reader.NameTable;
            var namespaceManager = new XmlNamespaceManager(nametable);
            Debug.Assert(_prefixes.Count == _namespaces.Count);
            for (var i = 0; i < _prefixes.Count; i++)
            {
                namespaceManager.AddNamespace(_prefixes[i], _namespaces[i]);
            }
            return (bool) root.XPathEvaluate(_xpath, namespaceManager);
        }
    }
}
