using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;


namespace UnitTests
{
    public class ExploreRoslyn
    {
        const string SOURCE_WITH_COMPILATION_ERROR = @"
            using System;
            Console.WriteLine(""Hello world"");
            Console.WrightLine(""foo bar"");
        ";

        const string SOURCE_WITH_RUNTIME_ERROR = @"
            stringList.Add(""1"");
            string s = null;
            stringList.Add(""2"");
            var i = s.Length;
            stringList.Add(""3"");
        ";

        const string SOURCE_WITHOUT_ERROR = @"
            var msg = ""no errors"";
            stringList.Add(msg);
        ";

        public List<string> stringList = new List<string>();

        [Fact]
        public async Task ScriptWithRuntimeError()
        {
            var code = @"
            var a = 0;
            var b = 1 / a;
            ";
            try
            {
                await CSharpScript.RunAsync(code);
            }
            catch (DivideByZeroException dbze)
            {
                Console.WriteLine(dbze.StackTrace);
            }
        }

        [Fact]
        public async Task ScriptWithWrappedRuntimeError()
        {
            var code = @"
            try  {
                var a = 0;
                var b = 1 / a;
            }
            catch (System.DivideByZeroException dbze)
            {
                System.Console.WriteLine(dbze.StackTrace);
            }
            ";
            await CSharpScript.RunAsync(code);
        }

        [Fact]
        public void RuntimeErrorInNormalCSharp()
        {
            try
            {
                var a = 0;
                var b = 1 / a;
            }
            catch (DivideByZeroException dbze)
            {
                Console.WriteLine(dbze.StackTrace);
            }
        }

        [Fact]
        public async Task ExploreCompileTimeError()
        {
            var exception = await Assert.ThrowsAsync<CompilationErrorException>(() => CompileAndRunAsync(SOURCE_WITH_COMPILATION_ERROR) );
            Assert.StartsWith("(4,21): error CS0117: 'Console' does not contain", exception.Message);
        }

        [Fact]
        public async Task ExploreRunTimeError()
        {
            var exception = await Assert.ThrowsAnyAsync<Exception>(() => CompileAndRunAsync(SOURCE_WITH_RUNTIME_ERROR));
            Assert.IsType<NullReferenceException>(exception);

            
            Assert.Equal(2, stringList.Count);
            Assert.Equal(new[] { "1", "2" }, stringList.ToArray());
        }

        [Fact]
        public async Task NoError()
        {
            await CompileAndRunAsync(SOURCE_WITHOUT_ERROR);
            Assert.Equal(1, stringList.Count);
            Assert.Equal("no errors", stringList[0]);
        }

        private Task CompileAndRunAsync(string sourceCode)
        {
            Assert.Equal(0, stringList.Count);
            return CSharpScript.RunAsync(sourceCode, globals: this);
        }
    }
}
