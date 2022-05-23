using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace MapAssist.Helpers
{
    public class Pattern
    {
        private readonly string _mask;
        private readonly byte[] _pattern;

        public Pattern(string pattern)
        {
            var cleanPattern = pattern
                .Replace("\\x", " ")
                .Replace("??", "?")
                .Trim()
                .Split(' ')
                .ToList();

            _mask = string.Join("", cleanPattern.Select(o => o == "?" ? "?" : "x"));
            cleanPattern = cleanPattern.Select(o => o == "?" ? "00" : o).ToList();

            _pattern = cleanPattern
                .Select(o => byte.Parse(o, NumberStyles.HexNumber))
                .ToArray();
        }

        public bool Match(byte[] data, int offset)
        {
            if (offset + _pattern.Length >= data.Length) return false;

            for (var i = 0; i < _pattern.Length; i++)
            {
                if (_mask[i] == '?') continue;

                if (data[offset + i] != _pattern[i]) return false;
            }

            return true;
        }

        public override string ToString()
        {
            return "Pattern: " + string.Join(" ", _mask.Select((c, i) => c == '?' ? "?" : _pattern[i].ToString("X").PadLeft(2, '0')));
        }
    }
}
