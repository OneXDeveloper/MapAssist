using MapAssist.Helpers;
using MapAssist.Settings;
using System.Collections.Generic;

namespace MapAssist.Types
{
    public enum ShrineType : byte
    {
        None,
        Refill,
        Health,
        Mana,
        HPXChange,
        ManaXChange,
        Armor,
        Combat,
        ResistFire,
        ResistCold,
        ResistLightning,
        ResistPoison,
        Skill,
        ManaRegen,
        Stamina,
        Experience,
        Shrine,
        Portal,
        Gem,
        Fire,
        Monster,
        Explosive,
        Poison
    };

    public static class Shrine
    {
        public static Dictionary<string, LocalizedObj> LocalizedShrines = new Dictionary<string, LocalizedObj>();

        public static string ShrineDisplayName(UnitObject obj)
        {
            if (!LocalizedShrines.TryGetValue($"ShrId{obj.ObjectData.InteractType}", out var localItem))
            {
                return "Shrine";
            }

            var lang = MapAssistConfiguration.Loaded.LanguageCode;
            var label = localItem.GetType().GetProperty(lang.ToString()).GetValue(localItem, null).ToString();

            if (lang == Locale.enUS)
            {
                var trim = " Shrine";
                if (label.Contains(trim))
                {
                    return label.Replace(trim, "");
                }
            }

            return label;
        }
    }
}
