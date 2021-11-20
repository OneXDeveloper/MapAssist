
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using MapAssist.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MapAssist.Helpers
{
    public class StringEnumConverter<EnumType> : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var enumType = (EnumType)value;

            writer.WriteValue(Enum.GetName(typeof(EnumType), enumType));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var enumType = (EnumType)Enum.Parse(typeof(EnumType), $"{reader.Value}");

            return enumType;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(EnumType);
        }
    }

    public class ColorConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var color = (Color)value;

            writer.WriteValue($"{color.R}, {color.G}, {color.B}, {color.A}");
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return ParseColor((string)reader.Value);
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Color);
        }

        public Color ParseColor(string value)
        {
            if (value.StartsWith("#"))
            {
                return ColorTranslator.FromHtml(value);
            }

            if (!value.Contains(","))
            {
                return Color.FromName(value);
            }

            var ints = value.Split(',').Select(o => int.Parse(o.Trim())).ToArray();
            switch (ints.Length)
            {
                case 4:
                    return Color.FromArgb(ints[0], ints[1], ints[2], ints[3]);
                case 3:
                    return Color.FromArgb(ints[0], ints[1], ints[2]);
            }

            return Color.FromName(value);
        }
    }

    public class AreaListConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var areaList = (List<Area>)value;
            var configurationString = "";

            foreach(var areaNumber in areaList) {
                configurationString += $"{AreaExtensions.Name(areaNumber)}, ";
            }

            writer.WriteValue(configurationString);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JArray array = JArray.Load(reader);
            var areasString = array.ToObject<IList<string>>();     
            var areas = areasString.Select(x => LookupAreaByName(x)).ToList();
            return areas;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Area[]);
        }

        public List<Area> ParseCommaSeparatedAreasByName(string areas)
        {
            return areas
                .Split(',')
                .Select(o => LookupAreaByName(o.Trim()))
                .Where(o => o != Area.None)
                .ToList();
        }

        public Area LookupAreaByName(string name)
        {
            return Enum.GetValues(typeof(Area)).Cast<Area>().FirstOrDefault(area => area.Name() == name);
        }
    }


}
