using MapAssist.Types;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace MapAssist.Helpers
{
    public static class Localization
    {
        public static LocalizationFileObj _localization;

        public static void LoadLocalizationFile()
        {
            var resString = Properties.Resources.Localization;

            using (var Stream = new MemoryStream(resString))
            {
                using (var streamReader = new StreamReader(Stream))
                {
                    var jsonString = streamReader.ReadToEnd();
                    _localization = JsonConvert.DeserializeObject<LocalizationFileObj>(jsonString);
                }
            }

            foreach (var item in _localization.Areas)
            {
                AreaExtensions.LocalizedAreas.Add(item.Key, item);
            }

            foreach (var item in _localization.Items)
            {
                Items.LocalizedItems.Add(item.Key, item);
            }

            foreach (var item in _localization.Runewords)
            {
                Items.LocalizedRunewords.Add((ushort)item.ID, item);
            }

            foreach (var item in _localization.Npcs)
            {
                NpcExtensions.LocalizedNpcs.Add(item.Key, item);
            }

            foreach (var item in _localization.Monsters)
            {
                NpcExtensions.LocalizedNpcs.Add(item.Key, item);
            }

            foreach (var item in _localization.Shrines)
            {
                Shrine.LocalizedShrines.Add(item.Key, item);
            }
        }
    }

    public class LocalizationFileObj
    {
        public List<LocalizedObj> Areas = new List<LocalizedObj>();
        public List<LocalizedObj> Items = new List<LocalizedObj>();
        public List<LocalizedObj> Npcs = new List<LocalizedObj>();
        public List<LocalizedObj> Shrines = new List<LocalizedObj>();
        public List<LocalizedObj> Monsters = new List<LocalizedObj>();
        public List<LocalizedObj> Runewords = new List<LocalizedObj>();
    }

    public class LocalizedObj
    {
        public int ID { get; set; }
        public string Key { get; set; }
        public string enUS { get; set; }
        public string zhTW { get; set; }
        public string deDE { get; set; }
        public string esES { get; set; }
        public string frFR { get; set; }
        public string itIT { get; set; }
        public string koKR { get; set; }
        public string plPL { get; set; }
        public string esMX { get; set; }
        public string jaJP { get; set; }
        public string ptBR { get; set; }
        public string ruRU { get; set; }
        public string zhCN { get; set; }
    }
}
