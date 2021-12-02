/**
 *   Copyright (C) 2021 okaygo
 *
 *   https://github.com/misterokaygo/MapAssist/
 *
 *  This program is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with this program.  If not, see <https://www.gnu.org/licenses/>.
 **/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using MapAssist.Files;
using MapAssist.Settings;
using MapAssist.Types;
using YamlDotNet.Serialization;

namespace MapAssist.Settings
{
    public class MapAssistConfiguration
    {
        public static MapAssistConfiguration Loaded { get; set; }
        public static void Load()
        {
            var fileName = $"./Config.yaml";
            Loaded = ConfigurationParser<MapAssistConfiguration>.ParseConfiguration(fileName);
        }

        public void Save()
        {
            new ConfigurationParser<MapAssistConfiguration>().SerializeToFile(this);
        }

        [YamlMember(Alias = "UpdateTime", ApplyNamingConventions = false)]
        public int UpdateTime { get; set; } = 30;

        [YamlMember(Alias = "PrefetchAreas", ApplyNamingConventions = false)]
        public Area[] PrefetchAreas { get; set; } = new Area[] { };

        [YamlMember(Alias = "HiddenAreas", ApplyNamingConventions = false)]
        public Area[] HiddenAreas { get; set; } = new Area[] { };

        [YamlMember(Alias = "ClearPrefetchedOnAreaChange", ApplyNamingConventions = false)]
        public bool ClearPrefetchedOnAreaChange { get; set; } = true;

        [YamlMember(Alias = "RenderingConfiguration", ApplyNamingConventions = false)]
        public RenderingConfiguration RenderingConfiguration { get; set; } = new RenderingConfiguration();

        [YamlMember(Alias = "MapConfiguration", ApplyNamingConventions = false)]
        public MapConfiguration MapConfiguration { get; set; } = MapConfiguration.defaults;

        [YamlMember(Alias = "MapColorConfiguration", ApplyNamingConventions = false)]
        public MapColorConfiguration MapColorConfiguration { get; set; } = MapColorConfiguration.defaults;

        [YamlMember(Alias = "HotkeyConfiguration", ApplyNamingConventions = false)]
        public HotkeyConfiguration HotkeyConfiguration { get; set; } = new HotkeyConfiguration();

        [YamlMember(Alias = "ApiConfiguration", ApplyNamingConventions = false)]
        public ApiConfiguration ApiConfiguration { get; set; } = new ApiConfiguration() { Endpoint = "http://localhost:8080/", Token = "" };

        [YamlMember(Alias = "GameInfo", ApplyNamingConventions = false)]
        public GameInfoConfiguration GameInfo { get; set; } = new GameInfoConfiguration() { ShowOverlayFPS = false };

        [YamlMember(Alias = "ItemLog", ApplyNamingConventions = false)]
        public ItemLogConfiguration ItemLog { get; set; } = new ItemLogConfiguration();
    }

    public class MapColorConfiguration
    {
        public static MapColorConfiguration defaults = new MapColorConfiguration()
        {
            MapColors = new Dictionary<int, Color?>()
            {
                { 0, Color.FromArgb(50, 50, 50) },
                { 2, Color.FromArgb(10, 51, 23) },
                { 3, Color.FromArgb(255, 255, 255) },
                { 4, Color.FromArgb(0, 255, 255) },
                { 6, Color.FromArgb(50, 50, 50) },
                { 7, Color.FromArgb(0, 0, 0) },
                { 16, Color.FromArgb(50, 50, 50) },
                { 17, Color.FromArgb(50, 50, 50) },
                { 19, Color.FromArgb(100, 100, 100) },
                { 20, Color.FromArgb(40, 40, 40) },
                { 21, Color.FromArgb(50, 50, 50) },
                { 23, Color.FromArgb(120, 120, 120) },
                { 33, Color.FromArgb(0, 0, 0) },
                { 37, Color.FromArgb(0, 0, 0) },
                { 39, Color.FromArgb(0, 0, 0) },
                { 53, Color.FromArgb(0, 0, 0) }
            }
        };

        [YamlMember(Alias = "MapColors", ApplyNamingConventions = false)]
        public Dictionary<int, Color?> MapColors { get; set; }

        public Color? LookupMapColor(int type)
        {
            if (!MapColors.ContainsKey(type))
            {
                MapColors[type] = null;
            }
            return MapColors[type];
        }

        public MapColorConfiguration()
        {
            MapColors = new Dictionary<int, Color?>();
        }
    }

    public class MapConfiguration
    {
        public static MapConfiguration defaults = new MapConfiguration()
        { 
            SuperUniqueMonster = new IconRendering() { IconColor = Color.Yellow, IconShape = Shape.SquareOutline, IconSize = 6, IconThickness = 2 },
            UniqueMonster = new IconRendering() { IconColor = Color.DarkOrange, IconShape = Shape.SquareOutline, IconSize = 6, IconThickness = 2 },
            EliteMonster = new IconRendering() { IconColor = Color.DarkRed, IconShape = Shape.SquareOutline, IconSize = 6, IconThickness = 2 },
            NormalMonster = new IconRendering() { IconColor = Color.Gray, IconShape = Shape.SquareOutline, IconSize = 6, IconThickness = 2 },
            NextArea = new PointOfInterestRendering() { IconColor = Color.FromArgb(237, 107, 0), IconShape = Shape.Square, IconSize = 10, LineColor = Color.Chartreuse, LineThickness = 2, ArrowHeadSize = 10, LabelColor = Color.Chartreuse, LabelFontSize = 8, LabelFont = "Helvetica" },
            PreviousArea = new PointOfInterestRendering() { IconColor = Color.FromArgb(255, 0, 149), IconShape = Shape.Square, IconSize = 10, LineColor = Color.Chartreuse, LineThickness = 2, ArrowHeadSize = 10, LabelColor = Color.Chartreuse, LabelFontSize = 8, LabelFont = "Helvetica" },
            Waypoint = new PointOfInterestRendering() { IconColor = Color.FromArgb(16, 140, 235), IconShape = Shape.Square, IconSize = 10 },
            Quest = new PointOfInterestRendering() { LineColor = Color.Chartreuse, LineThickness = 2, ArrowHeadSize = 5 },
            Player = new PointOfInterestRendering() { IconColor = Color.Yellow, IconShape = Shape.Square, IconSize = 5, IconThickness = 2 },
            SuperChest = new PointOfInterestRendering() { IconColor = Color.FromArgb(17, 255, 0), IconShape = Shape.Ellipse, IconSize = 10 },
            NormalChest = new PointOfInterestRendering() { IconColor = Color.FromArgb(96, 96, 96), IconShape = Shape.Square, IconSize = 5 },
            Shrine = new PointOfInterestRendering() { IconColor = Color.FromArgb(255, 218, 100), IconShape = Shape.Polygon, IconSize = 10 },
            Item = new PointOfInterestRendering() { IconColor = Color.DarkOrange, IconShape = Shape.Ellipse, IconSize = 6, LabelFont = "Helvetica", LabelFontSize = 8 }
        };

        [YamlMember(Alias = "SuperUniqueMonster", ApplyNamingConventions = false)]
        public IconRendering SuperUniqueMonster { get; set; }

        [YamlMember(Alias = "UniqueMonster", ApplyNamingConventions = false)]
        public IconRendering UniqueMonster { get; set; }

        [YamlMember(Alias = "EliteMonster", ApplyNamingConventions = false)]
        public IconRendering EliteMonster { get; set; }

        [YamlMember(Alias = "NormalMonster", ApplyNamingConventions = false)]
        public IconRendering NormalMonster { get; set; }

        [YamlMember(Alias = "NextArea", ApplyNamingConventions = false)]
        public PointOfInterestRendering NextArea { get; set; } 

        [YamlMember(Alias = "PreviousArea", ApplyNamingConventions = false)]
        public PointOfInterestRendering PreviousArea { get; set; } 

        [YamlMember(Alias = "Waypoint", ApplyNamingConventions = false)]
        public PointOfInterestRendering Waypoint { get; set; } 

        [YamlMember(Alias = "Quest", ApplyNamingConventions = false)]
        public PointOfInterestRendering Quest { get; set; } 

        [YamlMember(Alias = "Player", ApplyNamingConventions = false)]
        public PointOfInterestRendering Player { get; set; } 

        [YamlMember(Alias = "SuperChest", ApplyNamingConventions = false)]
        public PointOfInterestRendering SuperChest { get; set; }

        [YamlMember(Alias = "NormalChest", ApplyNamingConventions = false)]
        public PointOfInterestRendering NormalChest { get; set; } 

        [YamlMember(Alias = "Shrine", ApplyNamingConventions = false)]
        public PointOfInterestRendering Shrine { get; set; }

        [YamlMember(Alias = "ArmorWeapRack", ApplyNamingConventions = false)]
        public PointOfInterestRendering ArmorWeapRack { get; set; }

        [YamlMember(Alias = "Item", ApplyNamingConventions = false)]
        public PointOfInterestRendering Item { get; set; } 
    }
}

public class RenderingConfiguration
{
    [YamlMember(Alias = "OverlayMode", ApplyNamingConventions = false)]
    public bool OverlayMode { get; set; } = true;

    [YamlMember(Alias = "Position", ApplyNamingConventions = false)]
    public MapPosition Position { get; set; } = MapPosition.Center;

    [YamlMember(Alias = "Opacity", ApplyNamingConventions = false)]
    public double Opacity { get; set; } = 0.5;

    [YamlMember(Alias = "Size", ApplyNamingConventions = false)]
    public int Size { get; set; } = 450;

    [YamlMember(Alias = "Rotate", ApplyNamingConventions = false)]
    public bool Rotate { get; set; } = true;

    [YamlMember(Alias = "ZoomLevel", ApplyNamingConventions = false)]
    public float ZoomLevel { get; set; } = 1;

    [YamlMember(Alias = "AlwaysOnTop", ApplyNamingConventions = false)]
    public bool AlwaysOnTop { get; set; } = true;

    [YamlMember(Alias = "ToggleViaInGameMap", ApplyNamingConventions = false)]
    public bool ToggleViaInGameMap { get; set; } = true;
}

public class HotkeyConfiguration
{
    [YamlMember(Alias = "ToggleKey", ApplyNamingConventions = false)]
    public char ToggleKey { get; set; }

    [YamlMember(Alias = "ZoomInKey", ApplyNamingConventions = false)]
    public char ZoomInKey { get; set; }

    [YamlMember(Alias = "ZoomOutKey", ApplyNamingConventions = false)]
    public char ZoomOutKey { get; set; }
}

public class ApiConfiguration
{
    [YamlMember(Alias = "Endpoint", ApplyNamingConventions = false)]
    public string Endpoint { get; set; }

    [YamlMember(Alias = "Token", ApplyNamingConventions = false)]
    public string Token { get; set; }
}

public class GameInfoConfiguration
{
    [YamlMember(Alias = "AlwaysShow", ApplyNamingConventions = false)]
    public bool AlwaysShow { get; set; }

    [YamlMember(Alias = "ShowOverlayFPS", ApplyNamingConventions = false)]
    public bool ShowOverlayFPS { get; set; }
}

public class ItemLogConfiguration
{
    [YamlMember(Alias = "FilterFileName", ApplyNamingConventions = false)]
    public string FilterFileName { get; set; }

    [YamlMember(Alias = "PlaySoundOnDrop", ApplyNamingConventions = false)]
    public bool PlaySoundOnDrop { get; set; } = true;

    [YamlMember(Alias = "MaxSize", ApplyNamingConventions = false)]
    public int MaxSize { get; set; } = 10;

    [YamlMember(Alias = "AlwaysShow", ApplyNamingConventions = false)]
    public bool AlwaysShow { get; set; } = false;

    [YamlMember(Alias = "LabelFont", ApplyNamingConventions = false)]
    public string LabelFont { get; set; } = "Helvetica";

    [YamlMember(Alias = "LabelFontSize", ApplyNamingConventions = false)]
    public int LabelFontSize { get; set; } = 14;
}
