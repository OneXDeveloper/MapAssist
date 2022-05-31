using System.Collections.Generic;

namespace MapAssist.Types
{
    public class Affix
    {
        public string name { get; set; }
        public string value { get; set; }
    }

    public class JSONItem
    {
        public uint txtFileNo { get; set; }
        public string baseName { get; set; }
        public string quality { get; set; }
        public string fullName { get; set; }
        public string runeWord { get; set; }
        public bool ethereal { get; set; }
        public bool identified { get; set; }
        public int numSockets { get; set; }
        public Position position { get; set; }
        public string bodyLoc { get; set; }
        public List<Affix> affixes { get; set; }
    }

    public class Position
    {
        public uint x { get; set; }
        public uint y { get; set; }
    }

    public class ExportedItems
    {
        public List<UnitItem> equipped { get; set; }
        public List<UnitItem> inventory { get; set; }
        public List<UnitItem> mercenary { get; set; }
        public List<UnitItem> cube { get; set; }
        public List<UnitItem> personalStash { get; set; }
        public List<UnitItem> sharedStashTab1 { get; set; }
        public List<UnitItem> sharedStashTab2 { get; set; }
        public List<UnitItem> sharedStashTab3 { get; set; }
    }
}
