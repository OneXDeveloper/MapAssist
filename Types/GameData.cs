using GameOverlay.Drawing;
using MapAssist.Structs;
using System;
using System.Collections.Generic;

namespace MapAssist.Types
{
    public enum MenuPanelState : byte
    {
        ClosedAll = 0,
        RightOpen,
        LeftOpen,
        BothOpen
    }

    public class GameData
    {
        public Point PlayerPosition;
        public uint MapSeed;
        public Difficulty Difficulty;
        public Area Area;
        public IntPtr MainWindowHandle;
        public string PlayerName;
        public UnitPlayer PlayerUnit;
        public Dictionary<uint, UnitPlayer> Players;
        public UnitPlayer[] Corpses;
        public UnitMonster[] Monsters;
        public UnitMonster[] Mercs;
        public UnitObject[] Objects;
        public UnitMissile[] Missiles;
        public UnitItem[] Items;
        public UnitItem[] AllItems;
        public ItemLogEntry[] ItemLog;
        public Session Session;
        public Roster Roster;
        public MenuPanelState MenuPanelOpen;
        public MenuData MenuOpen;
        public Npc LastNpcInteracted;
        public int ProcessId;

        public bool HasGameChanged(GameData other)
        {
            if (other == null) return true;
            if (MapSeed != other.MapSeed) return true;
            if (Difficulty != other.Difficulty) return true;
            if (PlayerName != other.PlayerName) return true;
            return false;
        }

        public bool HasMapChanged(GameData other)
        {
            return HasGameChanged(other) || Area != other.Area;
        }

        public override string ToString()
        {
            return $"{nameof(PlayerPosition)}: {PlayerPosition}, {nameof(MapSeed)}: {MapSeed}, {nameof(Difficulty)}: {Difficulty}, {nameof(Area)}: {Area}, {nameof(MenuOpen.Map)}: {MenuOpen.Map}";
        }
    }
}
