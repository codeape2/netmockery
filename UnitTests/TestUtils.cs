using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace UnitTests
{
    static public class TestUtils
    {
        static public string P(string input) => input.Replace('/', Path.DirectorySeparatorChar);
        static public string RootedPath(string root, string rest)
        {
            if (IsWindows) // windows
            {
                return Path.Combine(root + ":\\", rest);
            }
            else                                     // linux
            {
                return Path.Combine("/" + root, rest);
            }
        }
        static public bool IsWindows => Path.DirectorySeparatorChar == '\\';
    }
}
