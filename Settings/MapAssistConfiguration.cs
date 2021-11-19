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
using System.Diagnostics;
using System.Drawing;
using System.Runtime.Serialization;
using MapAssist.Files;
using MapAssist.Helpers;
using MapAssist.Settings;
using MapAssist.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using ColorConverter = MapAssist.Helpers.ColorConverter;

namespace MapAssist.Settings
{
    public class MapAssistConfiguration
    {
        [JsonProperty("UpdateTime")]
        public int UpdateTime { get; set; }

        [JsonProperty("PrefetchAreas")]
        [JsonConverter(typeof(AreaListConverter))]
        public List<Area> PrefetchAreas { get; set; }

        [JsonProperty("HiddenAreas")]
        [JsonConverter(typeof(AreaListConverter))]
        public List<Area> HiddenAreas { get; set; }

        [JsonProperty("ClearPrefetchedOnAreaChange")]
        public bool ClearPrefetchedOnAreaChange { get; set; }

        [JsonProperty("RenderingConfiguration")]
        RenderingConfiguration RenderingConfiguration { get; set; }
        
        [JsonProperty("MonsterRendering")]
        MonsterRendering MonsterRendering { get; set; }

        [JsonProperty("MapConfiguration")]
        MapConfiguration MapConfiguration { get; set; }

        [JsonProperty("MapColorConfiguration")]
        MapColorConfiguration MapColorConfiguration { get; set; }

        [JsonProperty("HotkeyConfiguration")]
        HotkeyConfiguration HotkeyConfiguration { get; set; }

        [JsonProperty("ApiConfiguration")]
        ApiConfiguration ApiConfiguration { get; set; }

        public static MapAssistConfiguration Load()
        {
            return new ConfigurationParser<MapAssistConfiguration>().ParseConfiguration();
        }

        public void Save()
        {
            new ConfigurationParser<MapAssistConfiguration>().SerializeToFile(this);
        }

        [OnError]
        internal void OnError(StreamingContext context, ErrorContext errorContext)
        {
            Debug.WriteLine($"Serialization error in MapAssistConfiguration: {context.ToString()} {errorContext}");
            errorContext.Handled = true;
        }
    }

    public class MapColorConfiguration
    {
        [JsonProperty("MapColors")]
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

        [JsonProperty("NextArea")]
        public PointOfInterestRendering NextArea { get; set; }

        [JsonProperty("PreviousArea")]
        public PointOfInterestRendering PreviousArea { get; set; }

        [JsonProperty("Waypoint")]
        public PointOfInterestRendering Waypoint { get; set; }

        [JsonProperty("Quest")]
        public PointOfInterestRendering Quest { get; set; }

        [JsonProperty("Player")]
        public PointOfInterestRendering Player { get; set; }

        [JsonProperty("SuperChest")]
        public PointOfInterestRendering SuperChest { get; set; }

        [JsonProperty("Shrine")]
        public PointOfInterestRendering Shrine { get; set; }

        public MapConfiguration()
        {
            NextArea = new PointOfInterestRendering();
            PreviousArea = new PointOfInterestRendering();
            Waypoint = new PointOfInterestRendering();
            Quest = new PointOfInterestRendering();
            Player = new PointOfInterestRendering();
            SuperChest = new PointOfInterestRendering();
            Shrine = new PointOfInterestRendering();
        }
    }
}

public class MonsterRendering
{
    [JsonProperty("NormalColor")]
    [JsonConverter(typeof(ColorConverter))]
    public Color NormalColor;

    [JsonProperty("EliteColor")]
    [JsonConverter(typeof(ColorConverter))]
    public Color EliteColor;
}
public class RenderingConfiguration
{
    [JsonProperty("Opacity")]
    public double Opacity { get; set; }

    [JsonProperty("OverlayMode")]
    public bool OverlayMode { get; set; }

    [JsonProperty("AlwaysOnTop")]
    public bool AlwaysOnTop { get; set; }

    [JsonProperty("ToggleViaInGameMap")]
    public bool ToggleViaInGameMap { get; set; }

    [JsonProperty("Size")]
    public int Size { get; set; }

    [JsonProperty("Position")]
    [JsonConverter(typeof(StringEnumConverter<MapPosition>))]
    public MapPosition Position { get; set; }

    [JsonProperty("Rotate")]
    public bool Rotate { get; set; }

    [JsonProperty("ZoomLevel")]
    public float ZoomLevel { get; set; }

    public RenderingConfiguration()
    {
        Opacity = 0.7f;
        OverlayMode = true;
        AlwaysOnTop = true;
        ToggleViaInGameMap = true;
        Size = 450;
        Position = MapPosition.TopRight;
        Rotate = true;
        ZoomLevel = 1.0f;
    }
}

public class HotkeyConfiguration
{
    [JsonProperty("ToggleKey")]
    public char ToggleKey { get; set; }

    [JsonProperty("ZoomInKey")]
    public char ZoomInKey { get; set; }

    [JsonProperty("ZoomOutKey")]
    public char ZoomOutKey { get; set; }

    public HotkeyConfiguration()
    {
        ToggleKey = 'p';
        ZoomInKey = '+';
        ZoomOutKey = '-';
    }
}

public class ApiConfiguration
{
    [JsonProperty("Endpoint")]
    public string Endpoint { get; set; }

    [JsonProperty("ApiToken")]
    public string Token { get; set; }

    public ApiConfiguration()
    {
        Endpoint = "127.0.0.1:8080";
        Token = "";
    }
}
