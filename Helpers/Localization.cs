using MapAssist.Types;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace MapAssist.Helpers
{
    public static class Localization
    {
        public static List<LocalizedObj> _itemNames;
        public static List<LocalizedObj> _itemRunes;
        public static List<LocalizedObj> _levels;
        public static List<LocalizedObj> _monsters;
        public static List<LocalizedObj> _npcs;
        public static List<LocalizedObj> _objects;
        public static List<LocalizedObj> _shrines;
        public static List<LocalizedObj> _itemModifiers;

        public static void LoadLocalizationData()
        {
            LoadItemNames();
            LoadItemRunes();
            LoadLevels();
            LoadMonsters();
            LoadNpcs();
            LoadShrines();
            LoadObjects();
            LoadItemModifiers();
        }

        private static void LoadItemNames()
        {
            _itemNames = LoadObjectsFromResource(Properties.Resources.ItemNames);

            foreach (var item in _itemNames)
            {
                Items.LocalizedItems.Add(item.Key, item);
            }
        }

        private static void LoadItemRunes()
        {
            _itemRunes = LoadObjectsFromResource(Properties.Resources.ItemRunes);

            foreach (var item in _itemRunes)
            {
                if (item.Key.StartsWith("Runeword"))
                {
                    Items.LocalizedRunewords.Add((ushort)item.ID, item);
                }
                else
                {
                    Items.LocalizedRunes.Add(item.Key, item);
                }
            }
        }

        private static void LoadLevels()
        {
            _levels = LoadObjectsFromResource(Properties.Resources.Levels);

            foreach (var item in _levels)
            {
                AreaExtensions.LocalizedAreas.Add(item.Key, item);
            }
        }

        private static void LoadMonsters()
        {
            _monsters = LoadObjectsFromResource(Properties.Resources.Monsters);

            foreach (var item in _monsters)
            {
                NpcExtensions.LocalizedNpcs.Add(item.Key, item);
            }
        }

        private static void LoadNpcs()
        {
            _npcs = LoadObjectsFromResource(Properties.Resources.Npcs);

            foreach (var item in _npcs)
            {
                NpcExtensions.LocalizedNpcs.Add(item.Key, item);
            }
        }

        private static void LoadShrines()
        {
            _shrines = LoadObjectsFromResource(Properties.Resources.Shrines);

            foreach (var item in _shrines)
            {
                Shrine.LocalizedShrines.Add(item.Key, item);
            }
        }

        private static void LoadObjects()
        {
            _objects = LoadObjectsFromResource(Properties.Resources.Objects);

            foreach (var item in _objects)
            {
                GameObjects.LocalizedObjects.Add(item.Key, item);
            }
        }

        private static void LoadItemModifiers()
        {
            _itemModifiers = LoadObjectsFromResource(Properties.Resources.ItemModifiers);

            foreach (var item in _itemModifiers)
            {
                StatReader.LocalizedStatText.Add(item.Key, item);
            }
        }

        private static List<LocalizedObj> LoadObjectsFromResource(byte[] resString)
        {
            using (var Stream = new MemoryStream(resString))
            {
                using (var streamReader = new StreamReader(Stream))
                {
                    var jsonString = streamReader.ReadToEnd();
                    return JsonConvert.DeserializeObject<List<LocalizedObj>>(jsonString);
                }
            }
        }
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
