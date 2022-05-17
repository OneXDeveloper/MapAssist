using MapAssist.Helpers;
using MapAssist.Settings;
using MapAssist.Structs;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace MapAssist.Types
{
    public class UnitItem : UnitAny
    {
        public ItemData ItemData { get; private set; }
        public new bool IsPlayerOwned { get; set; } = false;
        public Npc VendorOwner { get; set; } = Npc.Invalid;
        public Item Item => (Item)TxtFileNo;
        public ItemMode ItemMode => (ItemMode)Struct.Mode;
        public string ItemBaseName => Items.GetItemBaseName(this);
        public ItemQuality? MappedItemQuality { get; set; } = null;

        public UnitItem(IntPtr ptrUnit) : base(ptrUnit)
        {
        }

        public new UnitItem Update()
        {
            if (base.Update() == UpdateResult.InvalidUpdate) return this;

            if (IsValidUnit && MapAssistConfiguration.Loaded.ItemLog.Enabled)
            {
                using (var processContext = GameManager.GetProcessContext())
                {
                    ItemData = processContext.Read<ItemData>(Struct.pUnitData);
                    MappedItemQuality = GetMappedItemQuality();
                }
            }

            return this;
        }

        private bool _isInvalid = false;

        public void MarkValid() => _isInvalid = false;

        public void MarkInvalid() => _isInvalid = true;

        public bool IsValidItem => !_isInvalid && UnitId != uint.MaxValue;

        public bool IsEthereal => (ItemData.ItemFlags & ItemFlags.IFLAG_ETHEREAL) == ItemFlags.IFLAG_ETHEREAL;

        public bool IsIdentified => ItemData.ItemQuality >= ItemQuality.MAGIC && (ItemData.ItemFlags & ItemFlags.IFLAG_IDENTIFIED) == ItemFlags.IFLAG_IDENTIFIED;

        public bool IsIdentifiedForLog { get; set; }

        public bool IsRuneWord => (ItemData.ItemFlags & ItemFlags.IFLAG_RUNEWORD) == ItemFlags.IFLAG_RUNEWORD;

        public bool IsDropped => ItemModeMapped == ItemModeMapped.Ground;

        public bool IsInStore => ItemModeMapped == ItemModeMapped.Vendor;

        public bool IsInSocket => ItemModeMapped == ItemModeMapped.Socket;

        public ushort[] Prefixes => ItemData.Affixes.Take(3).ToArray();

        public ushort[] Suffixes => ItemData.Affixes.Skip(3).ToArray();

        public StashTab StashTab { get; set; } = StashTab.None;

        public bool IsAnyPlayerHolding
        {
            get
            {
                switch (ItemModeMapped)
                {
                    case ItemModeMapped.Belt:
                    case ItemModeMapped.Inventory:
                    case ItemModeMapped.Cube:
                    case ItemModeMapped.Stash:
                    case ItemModeMapped.Player:
                    case ItemModeMapped.Mercenary:
                        return true;
                }
                return false;
            }
        }

        public ItemModeMapped ItemModeMapped
        {
            get
            {
                switch (ItemMode)
                {
                    case ItemMode.INBELT: return ItemModeMapped.Belt;
                    case ItemMode.DROPPING: return ItemModeMapped.Ground;
                    case ItemMode.ONGROUND: return ItemModeMapped.Ground;
                    case ItemMode.SOCKETED: return ItemModeMapped.Socket;
                    case ItemMode.EQUIP:
                        if (ItemData.dwOwnerID != uint.MaxValue) return ItemModeMapped.Player;
                        else return ItemModeMapped.Mercenary;
                }

                if (ItemData.dwOwnerID == uint.MaxValue && (ItemData.ItemFlags & ItemFlags.IFLAG_INSTORE) == ItemFlags.IFLAG_INSTORE && ItemData.InvPage != InvPage.NULL) return ItemModeMapped.Vendor;
                if (ItemData.dwOwnerID == uint.MaxValue) return ItemModeMapped.Selected;
                if (ItemData.dwOwnerID != uint.MaxValue && ItemData.InvPage == InvPage.EQUIP) return ItemModeMapped.Trade; // Other player's trade window

                switch (ItemData.InvPage)
                {
                    case InvPage.INVENTORY: return ItemModeMapped.Inventory;
                    case InvPage.TRADE: return ItemModeMapped.Trade;
                    case InvPage.CUBE: return ItemModeMapped.Cube;
                    case InvPage.STASH: return ItemModeMapped.Stash;
                }

                return ItemModeMapped.Unknown; // Items that appeared in the trade window before will appear here
            }
        }

        public Color ItemBaseColor
        {
            get
            {
                if (MappedItemQuality == null) return Color.Empty;

                var ItemColors = new Dictionary<ItemQuality, Color>()
                {
                    {ItemQuality.INFERIOR, Color.White},
                    {ItemQuality.NORMAL, Color.White},
                    {ItemQuality.SUPERIOR, MapAssistConfiguration.Loaded.ItemLog.SuperiorColor},
                    {ItemQuality.MAGIC, MapAssistConfiguration.Loaded.ItemLog.MagicColor},
                    {ItemQuality.RARE, MapAssistConfiguration.Loaded.ItemLog.RareColor},
                    {ItemQuality.SET, MapAssistConfiguration.Loaded.ItemLog.SetColor},
                    {ItemQuality.UNIQUE, MapAssistConfiguration.Loaded.ItemLog.UniqueColor},
                    {ItemQuality.CRAFTED, MapAssistConfiguration.Loaded.ItemLog.CraftedColor},
                };

                return ItemColors[(ItemQuality)MappedItemQuality];
            }
        }

        private ItemQuality? GetMappedItemQuality()
        {
            var ItemQualities = new List<ItemQuality>()
            {
                ItemQuality.INFERIOR,
                ItemQuality.NORMAL,
                ItemQuality.SUPERIOR,
                ItemQuality.MAGIC,
                ItemQuality.RARE,
                ItemQuality.SET,
                ItemQuality.UNIQUE,
                ItemQuality.CRAFTED,
            };

            if (!ItemQualities.Contains(ItemData.ItemQuality))
            {
                // Invalid item quality
                return null;
            }

            if (IsEthereal && ItemData.ItemQuality <= ItemQuality.NORMAL)
            {
                return ItemQuality.SUPERIOR;
            }

            if (Stats.ContainsKey(Types.Stats.Stat.NumSockets) && ItemData.ItemQuality <= ItemQuality.NORMAL)
            {
                return ItemQuality.SUPERIOR;
            }

            if (TxtFileNo >= 610 && TxtFileNo <= 642)
            {
                // Runes
                return ItemQuality.CRAFTED;
            }

            switch (TxtFileNo)
            {
                case 647: // Key of Terror
                case 648: // Key of Hate
                case 649: // Key of Destruction
                case 653: // Token of Absolution
                case 654: // Twisted Essence of Suffering
                case 655: // Charged Essense of Hatred
                case 656: // Burning Essence of Terror
                case 657: // Festering Essence of Destruction
                    return ItemQuality.CRAFTED;
            }

            return ItemData.ItemQuality;
        }

        public override string HashString => Item + "/" + Position.X + "/" + Position.Y;
    }
}
