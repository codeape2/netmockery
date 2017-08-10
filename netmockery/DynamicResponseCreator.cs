using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
#if !NET462
using System.Runtime.Loader;
#endif
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace netmockery
{
    public abstract class DynamicResponseCreatorBase : SimpleResponseCreator
    {
#if NET462
        private string _sourceAtCompilationTime;
        private Assembly _compiledAssembly;
        private Type _compiledType;
#endif

        public DynamicResponseCreatorBase(Endpoint endpoint) : base(endpoint) { }

        public virtual string FileSystemDirectory { get { return null; } }

#if NET462
        public override async Task<string> GetBodyAsync(RequestInfo requestInfo)
        {
            Debug.Assert(requestInfo != null);
            var sourceCode = SourceCode;
            if (_compiledType == null || _sourceAtCompilationTime != sourceCode)
            {
                //TODO: Debug logging of referenced assemblies
                var scriptOptions = ScriptOptions.Default.WithReferences(
                    MetadataReference.CreateFromFile(typeof(Enumerable).GetTypeInfo().Assembly.Location), // System.Linq
                    MetadataReference.CreateFromFile(typeof(System.Xml.Linq.XElement).GetTypeInfo().Assembly.Location), // System.Xml.Linq
                    MetadataReference.CreateFromFile(typeof(System.IO.File).GetTypeInfo().Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(System.Diagnostics.Debug).GetTypeInfo().Assembly.Location)
                );

                var script = CSharpScript.Create<string>(
                    FileSystemDirectory != null 
                        ?
                        ExecuteIncludes(
                            CreateCorrectPathsInLoadStatements(sourceCode, FileSystemDirectory),
                            FileSystemDirectory
                        )                        
                        : 
                        sourceCode,
                    scriptOptions,
                    globalsType: typeof(RequestInfo)
                );
                var compilation = script.GetCompilation();
                var diagnostics = compilation.GetDiagnostics();
                if (diagnostics.Count() > 0)
                {
                    var first = diagnostics[0];
                    throw new Exception($"{first.Location.GetLineSpan()}: {first.GetMessage()}");
                }
                
                var ilstream = new MemoryStream();
                var pdbstream = new MemoryStream();
                compilation.Emit(ilstream, pdbstream);
                _compiledAssembly = Assembly.Load(ilstream.ToArray(), pdbstream.ToArray());
                _compiledType = _compiledAssembly.GetType("Submission#0");
                _sourceAtCompilationTime = sourceCode;
            }
            Debug.Assert(_compiledType != null);
            var factory = _compiledType.GetMethod("<Factory>");
            var submissionArray = new object[2];
            submissionArray[0] = requestInfo;
            Task<string> task = (Task<string>)factory.Invoke(null, new object[] { submissionArray });
            return await task;
        }
#else
        public override async Task<string> GetBodyAsync(RequestInfo requestInfo)
        {
             //TODO: Only create script object if source has changed
             Debug.Assert(requestInfo != null);
 
             var scriptOptions = ScriptOptions.Default.WithReferences(
                 MetadataReference.CreateFromFile(typeof(Enumerable).GetTypeInfo().Assembly.Location), // System.Linq
                 MetadataReference.CreateFromFile(typeof(System.Xml.Linq.XElement).GetTypeInfo().Assembly.Location), // System.Xml.Linq
                 MetadataReference.CreateFromFile(typeof(System.IO.File).GetTypeInfo().Assembly.Location),
                 MetadataReference.CreateFromFile(typeof(System.Diagnostics.Debug).GetTypeInfo().Assembly.Location),
                 MetadataReference.CreateFromFile(typeof(System.Text.RegularExpressions.Regex).GetTypeInfo().Assembly.Location)
             );
 
             var script = CSharpScript.Create<string>(
                 FileSystemDirectory != null
                     ?
                     ExecuteIncludes(
                         CreateCorrectPathsInLoadStatements(SourceCode, FileSystemDirectory),
                         FileSystemDirectory
                     )
                     :
                     SourceCode,
                 scriptOptions,
                 globalsType: typeof(RequestInfo)
             );
             var result = await script.RunAsync(globals: requestInfo);
             return result.ReturnValue;
        }
#endif

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
