using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace UnitTests
{
    public class DirectoryCreator : IDisposable
    {
        private string _directoryName;

        public DirectoryCreator(string directoryName = null)
        {
            if (directoryName != null)
            {
                _directoryName = directoryName;
            }
            else
            {
                _directoryName = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            }
            
            Debug.Assert(!Directory.Exists(_directoryName));
            Directory.CreateDirectory(_directoryName);
        }

        public void AddFile(string filename, string content)
        {
            Directory.CreateDirectory(Path.Combine(_directoryName, Path.GetDirectoryName(filename)));
            File.WriteAllText(Path.Combine(_directoryName, filename), content);
        }

        public string DirectoryName => _directoryName;

        public void Dispose()
        {
            Directory.Delete(_directoryName, true);
        }
    }
}
