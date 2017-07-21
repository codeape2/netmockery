using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using netmockery;

namespace UnitTests
{
    public class TestExploreEncodings
    {
        [Fact]
        public void TheseEncodingNamesAreValid()
        {
            AssertIsValidEncodingName("utf-8");
            AssertIsValidEncodingName("latin1");
            AssertIsValidEncodingName("iso-8859-1");
            AssertIsValidEncodingName("ascii");
        }

        [Fact]
        public void Latin1IsTheSameAsISO88591()
        {
            Assert.Equal(Encoding.GetEncoding("latin1"), Encoding.GetEncoding("iso-8859-1"));
        }

        [Fact]
        public void TheseEncodingNamesAreNotValid()
        {
            AssertIsNotValidEncodingName("lkj");
            AssertIsNotValidEncodingName("foobar");
        }

        [Fact]
        public void AllExpectedEncodingNamesAreInvalid()
        {
            foreach (var name in InvalidNames)
            {
                AssertIsNotValidEncodingName(name);
                AssertIsNotValidEncodingName(name.ToUpper());
                AssertIsNotValidEncodingName(name.ToLower());
            }
        }

        [Fact]
        public void AllExpectedEncodingNamesAreValid()
        {
            var invalidNames = new List<string>();
            foreach (var name in ValidNames)
            {
                if (!IsValidEncodingName(name))
                {
                    invalidNames.Add(name);
                }
            }
            Assert.True(invalidNames.Count == 0, string.Join(", ", invalidNames.Take(10)));
            foreach (var name in ValidNames)
            {
                AssertIsValidEncodingName(name);
                AssertIsValidEncodingName(name.ToUpper());
                AssertIsValidEncodingName(name.ToLower());
            }
        }
        
        private void AssertIsValidEncodingName(string name)
        {
            Assert.True(IsValidEncodingName(name), $"Encoding {name} is not valid");
        }

        public bool IsValidEncodingName(string name)
        {
            try
            {
                Encoding.GetEncoding(name);
            }
            catch (Exception)
            {
                return false;
            }
            return true;

        }

        private void AssertIsNotValidEncodingName(string name)
        {
            Assert.False(IsValidEncodingName(name), $"Encoding {name} is valid, this is unexpected");
        }

#if NET462
        private string[] ValidNames => ValidNamesNet462;
        private string[] InvalidNames => InvalidNamesNet462;
#else
        private string[] ValidNames => ValidNamesDotNetCore;
        private string[] InvalidNames => InvalidNamesDotNetCore;
#endif

        public static string[] InvalidNamesDotNetCore = new[] {
            "ISO_8859-2:1987",
            "ISO_8859-3:1988",
            "ISO_8859-4:1988",
            "ISO_8859-5:1988",
            "ISO_8859-6:1987",
            "ISO_8859-7:1987",
            "ISO_8859-8:1988",
            "ISO_8859-9:1989",
            "Shift_JIS",
            "Extended_UNIX_Code_Packed_Format_for_Japanese",
            "DIN_66003",
            "NS_4551-1",
            "SEN_850200_B",
            "KS_C_5601-1987",
            "ISO-2022-KR",
            "EUC-KR",
            "ISO-2022-JP",
            "GB_2312-80",
            "ISO-8859-13",
            "ISO-8859-15",
            "GBK",
            "GB18030",
            "IBM850",
            "IBM862",
            "IBM-Thai",
            "GB2312",
            "Big5",
            "macintosh",
            "IBM037",
            "IBM273",
            "IBM277",
            "IBM278",
            "IBM280",
            "IBM284",
            "IBM285",
            "IBM290",
            "IBM297",
            "IBM420",
            "IBM423",
            "IBM424",
            "IBM437",
            "IBM500",
            "IBM852",
            "IBM855",
            "IBM857",
            "IBM860",
            "IBM861",
            "IBM863",
            "IBM864",
            "IBM865",
            "IBM869",
            "IBM870",
            "IBM871",
            "IBM880",
            "IBM905",
            "IBM1026",
            "KOI8-R",
            "HZ-GB-2312",
            "IBM866",
            "IBM775",
            "KOI8-U",
            "IBM00858",
            "IBM00924",
            "IBM01140",
            "IBM01141",
            "IBM01142",
            "IBM01143",
            "IBM01144",
            "IBM01145",
            "IBM01146",
            "IBM01147",
            "IBM01148",
            "IBM01149",
            "Big5-HKSCS",
            "windows-874",
            "windows-1250",
            "windows-1251",
            "windows-1252",
            "windows-1253",
            "windows-1254",
            "windows-1255",
            "windows-1256",
            "windows-1257",
            "windows-1258",
            "TIS-620"
        };

        public static string[] ValidNamesDotNetCore = new string[]
        {
            "US-ASCII",
            "ISO_8859-1:1987",
            "UNICODE-1-1-UTF-7",
            "UTF-8",
            "ISO-10646-UCS-2",
            "UTF-7",
            "UTF-16BE",
            "UTF-16LE",
            "UTF-16",
            "UTF-32",
            "UTF-32BE",
            "UTF-32LE",
        };

        // this list should reflect the list in documentation.md
        public static string[] ValidNamesNet462 = new string[]
        {
            "US-ASCII",
            "ISO_8859-1:1987",
            "UNICODE-1-1-UTF-7",
            "UTF-8",
            "ISO-10646-UCS-2",
            "UTF-7",
            "UTF-16BE",
            "UTF-16LE",
            "UTF-16",
            "UTF-32",
            "UTF-32BE",
            "UTF-32LE",
            "ISO_8859-2:1987",
            "ISO_8859-3:1988",
            "ISO_8859-4:1988",
            "ISO_8859-5:1988",
            "ISO_8859-6:1987",
            "ISO_8859-7:1987",
            "ISO_8859-8:1988",
            "ISO_8859-9:1989",
            "Shift_JIS",
            "Extended_UNIX_Code_Packed_Format_for_Japanese",
            "DIN_66003",
            "NS_4551-1",
            "SEN_850200_B",
            "KS_C_5601-1987",
            "ISO-2022-KR",
            "EUC-KR",
            "ISO-2022-JP",
            "GB_2312-80",
            "ISO-8859-13",
            "ISO-8859-15",
            "GBK",
            "GB18030",
            "IBM850",
            "IBM862",
            "IBM-Thai",
            "GB2312",
            "Big5",
            "macintosh",
            "IBM037",
            "IBM273",
            "IBM277",
            "IBM278",
            "IBM280",
            "IBM284",
            "IBM285",
            "IBM290",
            "IBM297",
            "IBM420",
            "IBM423",
            "IBM424",
            "IBM437",
            "IBM500",
            "IBM852",
            "IBM855",
            "IBM857",
            "IBM860",
            "IBM861",
            "IBM863",
            "IBM864",
            "IBM865",
            "IBM869",
            "IBM870",
            "IBM871",
            "IBM880",
            "IBM905",
            "IBM1026",
            "KOI8-R",
            "HZ-GB-2312",
            "IBM866",
            "IBM775",
            "KOI8-U",
            "IBM00858",
            "IBM00924",
            "IBM01140",
            "IBM01141",
            "IBM01142",
            "IBM01143",
            "IBM01144",
            "IBM01145",
            "IBM01146",
            "IBM01147",
            "IBM01148",
            "IBM01149",
            "Big5-HKSCS",
            "windows-874",
            "windows-1250",
            "windows-1251",
            "windows-1252",
            "windows-1253",
            "windows-1254",
            "windows-1255",
            "windows-1256",
            "windows-1257",
            "windows-1258",
            "TIS-620"
        };


        public static string[] InvalidNamesNet462 = new string[0];


    }
}
