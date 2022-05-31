using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace MapAssist.Helpers
{
    internal static class ExcelDataLoader
    {
        public static List<Dictionary<string, string>> Parse(string text)
        {
            var items = new List<Dictionary<string, string>>();
            
            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Split('\t').ToArray()).ToArray();
            var headers = lines[0].Select(x => Regex.Replace(x, @"\s+", "")).ToArray();

            foreach (var line in lines.Skip(1).ToArray())
            {
                if (line[0] == "Expansion") continue; // Blizz likes to insert stupid nonsense in their files

                items.Add(line.Select((item, index) => new { item, index }).ToDictionary(x => headers[x.index], x => x.item));
            }

            return items;
        }
    }
}
