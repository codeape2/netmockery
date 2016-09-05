using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace UnitTests
{
    public class TestDirectoryCreator
    {
        [Fact]
        public void DirectoryIsCreatedAndCanBeRemoved()
        {
            var dc = new DirectoryCreator();
            Assert.NotNull(dc.DirectoryName);
            Assert.True(Directory.Exists(dc.DirectoryName));

            dc.Remove();
            Assert.False(Directory.Exists(dc.DirectoryName));
        }
    }

    public class TestDirectoryCreatorContent  : IDisposable
    {
        private DirectoryCreator dc = new DirectoryCreator();

        public void Dispose()
        {
            dc.Remove();
        }

        [Fact]
        public void AddSingleFile()
        {
            dc.AddFile("test.txt", "Hello World!");

            Assert.True(File.Exists(Path.Combine(dc.DirectoryName, "test.txt")));
            Assert.Equal("Hello World!", File.ReadAllText(Path.Combine(dc.DirectoryName, "test.txt")));
        }

        [Fact]
        public void AddContentInDirectories()
        {
            dc.AddFile("test.txt", "");
            dc.AddFile("foo\\test.txt", "");
            dc.AddFile("foo\\test2.txt", "");
            dc.AddFile("bar\\test.txt", "");

            var allFiles = GetFilesFromDir(dc.DirectoryName).ToArray();
            Assert.Equal(4, allFiles.Length);

            Assert.Equal(
                new[] { "test.txt", "bar\\test.txt", "foo\\test.txt", "foo\\test2.txt" }, 
                from f in allFiles select f.Substring(dc.DirectoryName.Length + 1)
            );
        }

        private IEnumerable<string> GetFilesFromDir(string dir)
        {
            return Directory.EnumerateFiles(dir).Concat(Directory.EnumerateDirectories(dir).SelectMany(subdir => GetFilesFromDir(subdir)));
        }
    }
}
