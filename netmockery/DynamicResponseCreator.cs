using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace netmockery
{
    public abstract class DynamicResponseCreatorBase : SimpleResponseCreator
    {
        public DynamicResponseCreatorBase(Endpoint endpoint) : base(endpoint) { }

        public virtual string FileSystemDirectory { get { return null; } }

        public string GetSourceCodeWithIncludesExecuted()
        {
            var sourceCode = SourceCode;

            return 
                FileSystemDirectory != null
                        ?
                        ExecuteIncludes(
                            CreateCorrectPathsInLoadStatements(sourceCode, FileSystemDirectory),
                            FileSystemDirectory
                        )
                        :
                        sourceCode;
        }


        public static IEnumerable<MetadataReference> GetDefaultMetadataReferences() 
        {
            yield return MetadataReference.CreateFromFile(typeof(System.Console).GetTypeInfo().Assembly.Location);
            yield return MetadataReference.CreateFromFile(typeof(Enumerable).GetTypeInfo().Assembly.Location);
            yield return MetadataReference.CreateFromFile(typeof(System.Xml.Linq.XElement).GetTypeInfo().Assembly.Location);
            yield return MetadataReference.CreateFromFile(typeof(System.Xml.XmlNamespaceManager).GetTypeInfo().Assembly.Location);
            yield return MetadataReference.CreateFromFile(typeof(System.Xml.XPath.Extensions).GetTypeInfo().Assembly.Location);
            yield return MetadataReference.CreateFromFile(typeof(System.IO.File).GetTypeInfo().Assembly.Location);
            yield return MetadataReference.CreateFromFile(typeof(System.Diagnostics.Debug).GetTypeInfo().Assembly.Location);
            yield return MetadataReference.CreateFromFile(typeof(System.Text.RegularExpressions.Regex).GetTypeInfo().Assembly.Location);
            yield return MetadataReference.CreateFromFile(typeof(Newtonsoft.Json.JsonConvert).GetTypeInfo().Assembly.Location);
            yield return MetadataReference.CreateFromFile(typeof(Queue<>).GetTypeInfo().Assembly.Location);
            yield return MetadataReference.CreateFromFile(typeof(System.Dynamic.ExpandoObject).GetTypeInfo().Assembly.Location);
            yield return MetadataReference.CreateFromFile(typeof(Microsoft.CSharp.RuntimeBinder.RuntimeBinderException).GetTypeInfo().Assembly.Location);
            yield return MetadataReference.CreateFromFile(typeof(ExpressionType).GetTypeInfo().Assembly.Location);
        }
        
        public override async Task<string> GetBodyAsync(RequestInfo requestInfo)
        {
            var result = await GetBodyInnerAsync(requestInfo);
            GC.Collect();
            return result;
        }

        private async Task<string> GetBodyInnerAsync(RequestInfo requestInfo)
        {
            Debug.Assert(requestInfo != null);

            var script = CSharpScript.Create<string>(
                code: GetSourceCodeWithIncludesExecuted(),
                options: ScriptOptions.Default
                    .WithReferences(GetDefaultMetadataReferences().ToArray())
                    .WithEmitDebugInformation(true),
                globalsType: typeof(RequestInfo)
            );
            script.Compile();
            var runner = script.CreateDelegate();
            return await runner(requestInfo);
        }

        static public string CreateCorrectPathsInLoadStatements(string sourceCode, string directory)
        {
            Debug.Assert(sourceCode != null);
            Debug.Assert(directory != null);
            return Regex.Replace(
                sourceCode, 
                "#load \"(.*?)\"",
                mo => "#load \"" + Path.GetFullPath(Path.Combine(directory, mo.Groups[1].Value)) + "\""
            );
        }

        static public string ExecuteIncludes(string sourceCode, string directory)
        {
            Debug.Assert(sourceCode != null);
            Debug.Assert(directory != null);

            return Regex.Replace(
                sourceCode,
                "#include \"(.*?)\"",
                mo => File.ReadAllText(Path.GetFullPath(Path.Combine(directory, mo.Groups[1].Value)))
            );
        }

        public abstract string SourceCode { get; }

        protected override void SetStatusCode(RequestInfo requestInfo, IHttpResponseWrapper response)
        {
            Debug.Assert(requestInfo != null);
            Debug.Assert(response != null);
            if (requestInfo.StatusCode != RequestInfo.USE_CONFIGURED_STATUS_CODE)
            {
                response.HttpStatusCode = (HttpStatusCode)requestInfo.StatusCode;
            }
            else
            {
                base.SetStatusCode(requestInfo, response);
            }                           
        }

        protected override void SetContentType(RequestInfo requestInfo, IHttpResponseWrapper response)
        {
            Debug.Assert(requestInfo != null);
            Debug.Assert(response != null);
            if (requestInfo.ContentType != RequestInfo.USE_CONFIGURED_CONTENT_TYPE)
            {
                response.ContentType = requestInfo.ContentType;
            }
            else
            {
                base.SetContentType(requestInfo, response);
            }            
        }
    }

    public class LiteralDynamicResponseCreator : DynamicResponseCreatorBase
    {
        private string _sourceCode;

        public LiteralDynamicResponseCreator(string sourceCode, Endpoint endpoint) : base(endpoint)
        {
            _sourceCode = sourceCode;
        }

        public override string SourceCode => _sourceCode;
    }

    public class FileDynamicResponseCreator : DynamicResponseCreatorBase, IResponseCreatorWithFilename
    {
        private string _filename;

        public FileDynamicResponseCreator(string filename, Endpoint endpoint) : base(endpoint)
        {
            _filename = filename;
        }

        public string Filename => Path.Combine(Endpoint.Directory, ReplaceParameterReference(_filename));

        public override string SourceCode => File.ReadAllText(Filename);

        public override string FileSystemDirectory => Path.GetDirectoryName(Filename);

        public override string ToString() => $"Execute script {ReplaceParameterReference(_filename)}";
    }
}
