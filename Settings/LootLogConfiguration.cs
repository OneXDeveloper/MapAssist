using MapAssist.Files;
using MapAssist.Types;

using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.Serialization;

namespace MapAssist.Settings
{
    public class LootLogConfiguration
    {
        public static List<ItemFilter> Filters { get; set; }

        public static void Load()
        {
            Filters = new List<ItemFilter>();
            
            var yaml = ConfigurationParser<Dictionary<string, List<ItemFilter>>>.ParseConfiguration(
                $"./{MapAssistConfiguration.Loaded.ItemLog.FilterFileName}");
            
             foreach (var filter in yaml)
             {
                 if (filter.Value != null)
                 {
                     foreach (var innerFilter in filter.Value)
                     {
                         innerFilter.BaseName = filter.Key;
                         Filters.Add(innerFilter);
                     }
                 }
                 else
                 {
                     // Just matching on key and don't care about anything else
                     var parsedFilter = new ItemFilter() {BaseName = filter.Key};
                     Filters.Add(parsedFilter);
                 }
             }
        }
    }

    public class ItemFilter
    {
        [YamlIgnore]
        public string BaseName { get; set; }
        public ItemQuality[] Qualities { get; set; }
        public bool? Ethereal { get; set; }
        public int[] Sockets { get; set; }
    }
}
