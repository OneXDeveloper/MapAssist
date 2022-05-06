using System.Collections.Generic;

namespace MapAssist.Types
{
    public enum Locale : uint
    {
        enUS,
        esMX,
        ptBR,
        frFR,
        deDE,
        esES,
        itIT,
        ruRU,
        plPL,
        koKR,
        jaJP,
        zhCN,
    };

    public static class LocaleExtensions
    {
        private readonly static Dictionary<Locale, string> _localeNames = new Dictionary<Locale, string>()
        {
            [Locale.enUS] = "English (United States)",
            [Locale.esMX] = "Spanish (Mexico)",
            [Locale.ptBR] = "Portuguese (Brazil)",
            [Locale.frFR] = "French (France)",
            [Locale.deDE] = "German (Germany)",
            [Locale.esES] = "Spanish (Spain)",
            [Locale.itIT] = "Italian (Italy)",
            [Locale.ruRU] = "Russian (Russia)",
            [Locale.plPL] = "Polish (Poland)",
            [Locale.koKR] = "Korean (South Korea)",
            [Locale.jaJP] = "Japanese (Japan)",
            [Locale.zhCN] = "Chinese (China)",
        };

        public static string Name(this Locale locale)
        {
            return _localeNames.TryGetValue(locale, out var localeName) ? localeName : locale.ToString();
        }

        public static bool IsValid(this Locale locale)
        {
            var intLocale = (uint)locale;
            return intLocale >= 0 && intLocale <= 11;
        }
    }
}
