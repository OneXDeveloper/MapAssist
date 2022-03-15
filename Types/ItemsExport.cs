using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    public class JSONItems
    {
        public List<JSONItem> equipped { get; set; }
        public List<JSONItem> inventory { get; set; }
        public List<JSONItem> mercenary { get; set; }
        public List<JSONItem> personalStash { get; set; }
        public List<JSONItem> sharedStashTab1 { get; set; }
        public List<JSONItem> sharedStashTab2 { get; set; }
        public List<JSONItem> sharedStashTab3 { get; set; }
    }

    public class ItemsExport
    {
        public JSONItems items { get; set; }
    }
}
