using MapAssist.Files;
using MapAssist.Types;

using System.Collections.Generic;

namespace MapAssist.Settings
{
    public class LootLogConfiguration
    {
        public static Dictionary<string, List<ItemFilter>> Filters { get; set; }

        public static void Load()
        {
            Filters = ConfigurationParser<Dictionary<string, List<ItemFilter>>>.ParseConfigurationFile($"./{MapAssistConfiguration.Loaded.ItemLog.FilterFileName}");
            
        }
    }

    public class ItemFilter
    {
        public ItemQuality[] Qualities { get; set; }
        public bool? Ethereal { get; set; }
        public int[] Sockets { get; set; }
    }
}
