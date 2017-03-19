using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace netmockery
{
    public abstract class DynamicResponseCreatorBase : SimpleResponseCreator
    {
        private string _sourceAtCompilationTime;
        private Assembly _compiledAssembly;
        private Type _compiledType;

        public DynamicResponseCreatorBase(Endpoint endpoint) : base(endpoint) { }

        public virtual string FileSystemDirectory { get { return null; } }

        public string Evaluate(RequestInfo requestInfo)
        {
            return Task.Run(async () => await EvaluateAsync(requestInfo)).Result;
        }

        public async Task<string> EvaluateAsync(RequestInfo requestInfo)
        {
            Debug.Assert(requestInfo != null);
            var sourceCode = SourceCode;

            if (_compiledType == null || _sourceAtCompilationTime != sourceCode)
            {
                var scriptOptions = ScriptOptions.Default
                    .WithReferences(
                        typeof(Enumerable).Assembly, // System.Core
                        typeof(System.Xml.Linq.XElement).Assembly // System.Xml.Linq
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
                _compiledAssembly = Assembly.Load(ilstream.GetBuffer(), pdbstream.GetBuffer());
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

        public override string GetBody(RequestInfo requestInfo) => Evaluate(requestInfo);

        public abstract string SourceCode { get; }
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
        private string _directory;
        private string _filename;

        public FileDynamicResponseCreator(string directory, string filename, Endpoint endpoint) : base(endpoint)
        {
            _directory = directory;
            _filename = filename;
        }

        public string Filename => Path.Combine(_directory, ReplaceParameterReference(_filename));

        public override string SourceCode => File.ReadAllText(Filename);

        public override string FileSystemDirectory => Path.GetDirectoryName(Filename);

        public override string ToString() => $"Execute script {Path.GetFileName(_filename)}";
    }

    public class AssemblyResponseCreator : SimpleResponseCreator
    {
        public AssemblyResponseCreator(Endpoint endpoint) : base(endpoint) { }
        public string AssemblyFilename { get; set; }
        public Assembly Assembly { get; set; }
        public string ClassName { get; set; }
        public string MethodName { get; set; }
        public override string GetBody(RequestInfo requestInfo)
        {
            if (Assembly == null)
            {
                Assembly = Assembly.LoadFile(AssemblyFilename);
            }
            var type = Assembly.GetType(ClassName);
            var methodInfo = type.GetMethod(MethodName, BindingFlags.Public | BindingFlags.Static);
            return (string)methodInfo.Invoke(null, new object[] { requestInfo.RequestPath, requestInfo.RequestBody });
        }
    }
}
