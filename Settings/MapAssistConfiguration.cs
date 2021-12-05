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

            var fileNameCustom = $"./Config_Custom.yaml";
            var Custom = ConfigurationParser<MapAssistConfiguration>.ParseConfiguration(fileNameCustom);
            Merge(Loaded, Custom);
            Console.WriteLine("merged configs");
        }

        public static void Merge(MapAssistConfiguration configPrimary, MapAssistConfiguration configOverride)
        {
            if (configPrimary == null || configOverride == null)
            {
                return;
            }
            var macProps = typeof(MapAssistConfiguration).GetProperties();
            foreach (var macProp in macProps)
            {
                // Ignore this prop, we don't want to set it.
                if (macProp.Name == "Loaded")
                {
                    continue;
                }
                // Deal with types that don't have additional properties.
                // We don't want to just overwrite a complex datatype if one prop
                // exists on the override config but nothing else.
                if (macProp.PropertyType != typeof(RenderingConfiguration)
                    && macProp.PropertyType != typeof(MapConfiguration)
                    && macProp.PropertyType != typeof(MapColorConfiguration)
                    && macProp.PropertyType != typeof(HotkeyConfiguration)
                    && macProp.PropertyType != typeof(ApiConfiguration)
                    && macProp.PropertyType != typeof(GameInfoConfiguration)
                    && macProp.PropertyType != typeof(ItemLogConfiguration))
                {
                    var valueOverride = macProp.GetValue(configOverride, null);
                    if (valueOverride != null)
                    {
                        macProp.SetValue(configPrimary, valueOverride);
                    }
                }
                if (macProp.PropertyType == typeof(RenderingConfiguration) && configOverride.RenderingConfiguration != null)
                {
                    SetProperties<RenderingConfiguration>(configPrimary.RenderingConfiguration, configOverride.RenderingConfiguration);
                }
                if (macProp.PropertyType == typeof(MapConfiguration) && configOverride.MapConfiguration != null)
                {
                    var mcProps = typeof(MapConfiguration).GetProperties();
                    foreach (var mcProp in mcProps)
                    {
                        switch (mcProp.Name)
                        {
                            case "SuperUniqueMonster":
                                SetProperties<IconRendering>(configPrimary.MapConfiguration.SuperUniqueMonster, configOverride.MapConfiguration.SuperUniqueMonster);
                                break;
                            case "UniqueMonster":
                                SetProperties<IconRendering>(configPrimary.MapConfiguration.UniqueMonster, configOverride.MapConfiguration.UniqueMonster);
                                break;
                            case "EliteMonster":
                                SetProperties<IconRendering>(configPrimary.MapConfiguration.EliteMonster, configOverride.MapConfiguration.EliteMonster);
                                break;
                            case "NormalMonster":
                                SetProperties<IconRendering>(configPrimary.MapConfiguration.NormalMonster, configOverride.MapConfiguration.NormalMonster);
                                break;
                            case "NextArea":
                                SetProperties<PointOfInterestRendering>(configPrimary.MapConfiguration.NextArea, configOverride.MapConfiguration.NextArea);
                                break;
                            case "PreviousArea":
                                SetProperties<PointOfInterestRendering>(configPrimary.MapConfiguration.PreviousArea, configOverride.MapConfiguration.PreviousArea);
                                break;
                            case "Waypoint":
                                SetProperties<PointOfInterestRendering>(configPrimary.MapConfiguration.Waypoint, configOverride.MapConfiguration.Waypoint);
                                break;
                            case "Quest":
                                SetProperties<PointOfInterestRendering>(configPrimary.MapConfiguration.Quest, configOverride.MapConfiguration.Quest);
                                break;
                            case "Player":
                                SetProperties<PointOfInterestRendering>(configPrimary.MapConfiguration.Player, configOverride.MapConfiguration.Player);
                                break;
                            case "SuperChest":
                                SetProperties<PointOfInterestRendering>(configPrimary.MapConfiguration.SuperChest, configOverride.MapConfiguration.SuperChest);
                                break;
                            case "NormalChest":
                                SetProperties<PointOfInterestRendering>(configPrimary.MapConfiguration.NormalChest, configOverride.MapConfiguration.NormalChest);
                                break;
                            case "Shrine":
                                SetProperties<PointOfInterestRendering>(configPrimary.MapConfiguration.Shrine, configOverride.MapConfiguration.Shrine);
                                break;
                            case "ArmorWeapRack":
                                SetProperties<PointOfInterestRendering>(configPrimary.MapConfiguration.ArmorWeapRack, configOverride.MapConfiguration.ArmorWeapRack);
                                break;
                            case "Item":
                                SetProperties<PointOfInterestRendering>(configPrimary.MapConfiguration.Item, configOverride.MapConfiguration.Item);
                                break;
                        }
                    }
                }
                // For now just replacing the entire struct if its defined in override.
                if (macProp.PropertyType == typeof(MapColorConfiguration) && configOverride.MapColorConfiguration != null)
                {
                    if (configOverride.MapColorConfiguration.MapColors != null)
                    {
                        configPrimary.MapColorConfiguration = configOverride.MapColorConfiguration;
                    }
                }
                if (macProp.PropertyType == typeof(HotkeyConfiguration) && configOverride.HotkeyConfiguration != null)
                {
                    SetProperties<HotkeyConfiguration>(configPrimary.HotkeyConfiguration, configOverride.HotkeyConfiguration);
                }
                if (macProp.PropertyType == typeof(ApiConfiguration) && configOverride.ApiConfiguration != null)
                {
                    SetProperties<ApiConfiguration>(configPrimary.ApiConfiguration, configOverride.ApiConfiguration);
                }
                if (macProp.PropertyType == typeof(GameInfoConfiguration) && configOverride.ApiConfiguration != null)
                {
                    SetProperties<GameInfoConfiguration>(configPrimary.GameInfo, configOverride.GameInfo);
                }
                if (macProp.PropertyType == typeof(ItemLogConfiguration) && configOverride.ItemLog != null)
                {
                    SetProperties<ItemLogConfiguration>(configPrimary.ItemLog, configOverride.ItemLog);
                }
            }
            return;
        }

        public static void SetProperties<T>(T primary, T custom)
        {
            if (custom == null)
            {
                return;
            }
            var props = typeof(T).GetProperties();
            foreach (var prop in props)
            {
                var valueOverride = prop.GetValue(custom, null);
                if (valueOverride != null)
                {
                    prop.SetValue(primary, valueOverride);
                }
            }
        }


        public void Save()
        {
            new ConfigurationParser<MapAssistConfiguration>().SerializeToFile(this);
        }

        [YamlMember(Alias = "UpdateTime", ApplyNamingConventions = false)]
        public int? UpdateTime { get; set; }

        [YamlMember(Alias = "HuntingIP", ApplyNamingConventions = false)]
        public string HuntingIP { get; set; }

        [YamlMember(Alias = "PrefetchAreas", ApplyNamingConventions = false)]
        public Area[] PrefetchAreas { get; set; }

        [YamlMember(Alias = "HiddenAreas", ApplyNamingConventions = false)]
        public Area[] HiddenAreas { get; set; }

        [YamlMember(Alias = "ClearPrefetchedOnAreaChange", ApplyNamingConventions = false)]
        public bool? ClearPrefetchedOnAreaChange { get; set; }

        [YamlMember(Alias = "RenderingConfiguration", ApplyNamingConventions = false)]
        public RenderingConfiguration RenderingConfiguration { get; set; }

        [YamlMember(Alias = "MapConfiguration", ApplyNamingConventions = false)]
        public MapConfiguration MapConfiguration { get; set; }

        [YamlMember(Alias = "MapColorConfiguration", ApplyNamingConventions = false)]
        public MapColorConfiguration MapColorConfiguration { get; set; }

        [YamlMember(Alias = "HotkeyConfiguration", ApplyNamingConventions = false)]
        public HotkeyConfiguration HotkeyConfiguration { get; set; }

        [YamlMember(Alias = "ApiConfiguration", ApplyNamingConventions = false)]
        public ApiConfiguration ApiConfiguration { get; set; }

        [YamlMember(Alias = "GameInfo", ApplyNamingConventions = false)]
        public GameInfoConfiguration GameInfo { get; set; }

        [YamlMember(Alias = "ItemLog", ApplyNamingConventions = false)]
        public ItemLogConfiguration ItemLog { get; set; }
    }

    public class MapColorConfiguration
    {
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
    [YamlMember(Alias = "Opacity", ApplyNamingConventions = false)]
    public double? Opacity { get; set; }

    [YamlMember(Alias = "OverlayMode", ApplyNamingConventions = false)]
    public bool? OverlayMode { get; set; }

    [YamlMember(Alias = "AlwaysOnTop", ApplyNamingConventions = false)]
    public bool? AlwaysOnTop { get; set; }

    [YamlMember(Alias = "ToggleViaInGameMap", ApplyNamingConventions = false)]
    public bool? ToggleViaInGameMap { get; set; }

    [YamlMember(Alias = "Size", ApplyNamingConventions = false)]
    public int? Size { get; set; }

    [YamlMember(Alias = "Position", ApplyNamingConventions = false)]
    public MapPosition? Position { get; set; }

    [YamlMember(Alias = "BuffPosition", ApplyNamingConventions = false)]
    public BuffPosition? BuffPosition { get; set; }

    [YamlMember(Alias = "BuffSize", ApplyNamingConventions = false)]
    public float? BuffSize { get; set; }

    [YamlMember(Alias = "Rotate", ApplyNamingConventions = false)]
    public bool? Rotate { get; set; }

    [YamlMember(Alias = "ZoomLevel", ApplyNamingConventions = false)]
    public float? ZoomLevel { get; set; }
}

public class HotkeyConfiguration
{
    [YamlMember(Alias = "ToggleKey", ApplyNamingConventions = false)]
    public char? ToggleKey { get; set; }

    [YamlMember(Alias = "ZoomInKey", ApplyNamingConventions = false)]
    public char? ZoomInKey { get; set; }

    [YamlMember(Alias = "ZoomOutKey", ApplyNamingConventions = false)]
    public char? ZoomOutKey { get; set; }

    [YamlMember(Alias = "GameInfoKey", ApplyNamingConventions = false)]
    public char? GameInfoKey { get; set; }
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
    [YamlMember(Alias = "Enabled", ApplyNamingConventions = false)]
    public bool? Enabled { get; set; }

    [YamlMember(Alias = "ShowOverlayFPS", ApplyNamingConventions = false)]
    public bool? ShowOverlayFPS { get; set; }
}

public class ItemLogConfiguration
{
    [YamlMember(Alias = "Enabled", ApplyNamingConventions = false)]
    public bool? Enabled { get; set; }

    [YamlMember(Alias = "FilterFileName", ApplyNamingConventions = false)]
    public string FilterFileName { get; set; }

    [YamlMember(Alias = "PlaySoundOnDrop", ApplyNamingConventions = false)]
    public bool? PlaySoundOnDrop { get; set; }

    [YamlMember(Alias = "DisplayForSeconds", ApplyNamingConventions = false)]
    public double? DisplayForSeconds { get; set; }
    [YamlMember(Alias = "SoundFile", ApplyNamingConventions = false)]
    public string SoundFile { get; set; }

    [YamlMember(Alias = "LabelFont", ApplyNamingConventions = false)]
    public string LabelFont { get; set; }

    [YamlMember(Alias = "LabelFontSize", ApplyNamingConventions = false)]
    public int? LabelFontSize { get; set; }
}
