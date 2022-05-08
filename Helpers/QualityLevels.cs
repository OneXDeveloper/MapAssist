using MapAssist.Types;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace MapAssist.Helpers
{
    public static class QualityLevels
    {
        public static List<QualityLevelsObj> _qualityLevels;

        public static void LoadQualityLevelsFile()
        {
            var resString = Properties.Resources.QualityLevels;

            using (var Stream = new MemoryStream(resString))
            {
                using (var streamReader = new StreamReader(Stream))
                {
                    _qualityLevels = JsonConvert.DeserializeObject<List<QualityLevelsObj>>(streamReader.ReadToEnd());
                }
            }

            foreach (var item in _qualityLevels)
            {
                var test = item.name;
                Items.QualityLevels.Add(item.key, item);
            }
        }
    }

    public class QualityLevelsObj
    {
        public int id { get; set; }
        public string key { get; set; }
        public string name { get; set; }
        public int qlvl { get; set; }
        public string tclass { get; set; }
    }
}
