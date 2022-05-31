using MapAssist.Settings;
using MapAssist.Types;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace MapAssist.Helpers
{
    internal sealed class FloatPrecisionConverter : IYamlTypeConverter
    {
        public bool Accepts(Type type)
        {
            return type == typeof(double);
        }

        public object ReadYaml(IParser parser, Type type)
        {
            throw new NotImplementedException();
        }

        public void WriteYaml(IEmitter emitter, object value, Type type)
        {
            emitter.Emit(new Scalar(null, ((double)value).ToString(new CultureInfo("en-US")))); // Otherwise some bug in the yamlconverter won't have the right precisions on doubles
        }
    }

    internal sealed class ColorConfigurationTypeConverter : IYamlTypeConverter
    {
        public bool Accepts(Type type)
        {
            return type == typeof(Color) || type == typeof(Color?);
        }

        public object ReadYaml(IParser parser, Type type)
        {
            throw new NotImplementedException();
        }

        public void WriteYaml(IEmitter emitter, object value, Type type)
        {
            if (value != null)
            {
                emitter.Emit(new Scalar(null, Helpers.GetColorName((Color)value)));
            }
        }
    }

    internal sealed class MapColorConfigurationTypeConverter : IYamlTypeConverter
    {
        public bool Accepts(Type type)
        {
            return type == typeof(MapColorConfiguration);
        }

        public object ReadYaml(IParser parser, Type type)
        {
            throw new NotImplementedException();
        }

        public void WriteYaml(IEmitter emitter, object value, Type type)
        {
            emitter.Emit(new MappingStart(null, null, false, MappingStyle.Block));

            var node = (MapColorConfiguration)value;
            if (node.Walkable != null)
            {
                emitter.Emit(new Scalar(null, "Walkable"));
                emitter.Emit(new Scalar(null, Helpers.GetColorName((Color)node.Walkable)));
            }
            if (node.Border != null)
            {
                emitter.Emit(new Scalar(null, "Border"));
                emitter.Emit(new Scalar(null, Helpers.GetColorName((Color)node.Border)));
            }

            emitter.Emit(new MappingEnd());
        }
    }

    internal sealed class PortalRenderingTypeConverter : IYamlTypeConverter
    {
        public bool Accepts(Type type)
        {
            return type == typeof(PortalRendering);
        }

        public object ReadYaml(IParser parser, Type type)
        {
            throw new NotImplementedException();
        }

        public void WriteYaml(IEmitter emitter, object value, Type type)
        {
            emitter.Emit(new MappingStart(null, null, false, MappingStyle.Block));

            Helpers.WritePOIRendering(emitter, (PortalRendering)value);

            emitter.Emit(new MappingEnd());
        }
    }

    internal sealed class PointOfInterestRenderingTypeConverter : IYamlTypeConverter
    {
        public bool Accepts(Type type)
        {
            return type == typeof(PointOfInterestRendering);
        }

        public object ReadYaml(IParser parser, Type type)
        {
            throw new NotImplementedException();
        }

        public void WriteYaml(IEmitter emitter, object value, Type type)
        {
            emitter.Emit(new MappingStart(null, null, false, MappingStyle.Block));

            Helpers.WritePOIRendering(emitter, (PointOfInterestRendering)value);

            emitter.Emit(new MappingEnd());
        }
    }

    internal sealed class IconRenderingTypeConverter : IYamlTypeConverter
    {
        public bool Accepts(Type type)
        {
            return type == typeof(IconRendering);
        }

        public object ReadYaml(IParser parser, Type type)
        {
            throw new NotImplementedException();
        }

        public void WriteYaml(IEmitter emitter, object value, Type type)
        {
            emitter.Emit(new MappingStart(null, null, false, MappingStyle.Block));

            Helpers.WriteIconRendering(emitter, (IconRendering)value);

            emitter.Emit(new MappingEnd());
        }
    }

    internal sealed class AreaArrayYamlTypeConverter : IYamlTypeConverter
    {
        public bool Accepts(Type type)
        {
            return type == typeof(Area[]);
        }

        public object ReadYaml(IParser parser, Type type)
        {
            if (parser.TryConsume<Scalar>(out var scalar))
            {
                var item = new List<string> { scalar.Value };
                return ParseAreaStringList(item);
            }

            if (parser.TryConsume<SequenceStart>(out var _))
            {
                var items = new List<string>();
                while (parser.TryConsume<Scalar>(out var scalarItem))
                {
                    items.Add(scalarItem.Value);
                }
                parser.Consume<SequenceEnd>();
                return ParseAreaStringList(items); ;
            }
            return new Area[0];
        }

        public void WriteYaml(IEmitter emitter, object value, Type type)
        {
            var node = (Area[])value;
            emitter.Emit(new SequenceStart(null, null, false, SequenceStyle.Block));

            foreach (var child in node)
            {
                emitter.Emit(new Scalar(null, child.NameInternal()));
            }

            emitter.Emit(new SequenceEnd());
        }

        private Area[] ParseAreaStringList(List<string> areas)
        {
            return areas
                .Select(o => LookupAreaByName(o.Trim()))
                .Where(o => o != Area.None)
                .ToArray();
        }

        private Area LookupAreaByName(string name)
        {
            return Enum.GetValues(typeof(Area)).Cast<Area>().FirstOrDefault(area => area.NameInternal().ToLower() == name.ToLower());
        }
    }

    internal sealed class ItemYamlTypeConverter : IYamlTypeConverter
    {
        public bool Accepts(Type type)
        {
            return type == typeof(Item);
        }

        public object ReadYaml(IParser parser, Type type)
        {
            if (parser.TryConsume<Scalar>(out var scalar))
            {
                if (Enum.TryParse("Class" + scalar.Value.Replace(" ", "").Replace("-", ""), true, out Item itemClass))
                {
                    return itemClass;
                }
                else if (Enum.TryParse(scalar.Value.Replace(" ", "").Replace("-", ""), true, out Item item))
                {
                    return item;
                }
                else
                {
                    throw new Exception($"Failed to parse item: {scalar.Value}");
                }
            }

            return null;
        }

        public void WriteYaml(IEmitter emitter, object value, Type type)
        {
            throw new NotImplementedException();
        }
    }

    internal sealed class ItemQualityYamlTypeConverter : IYamlTypeConverter
    {
        public bool Accepts(Type type)
        {
            return type == typeof(ItemQuality[]);
        }

        public object ReadYaml(IParser parser, Type type)
        {
            if (parser.TryConsume<Scalar>(out var scalar))
            {
                var items = new List<string>() { scalar.Value };
                return ParseItemQuality(items);
            }

            if (parser.TryConsume<SequenceStart>(out var _))
            {
                var items = new List<string>();
                while (parser.TryConsume<Scalar>(out var scalarItem))
                {
                    items.Add(scalarItem.Value);
                }

                parser.Consume<SequenceEnd>();
                return ParseItemQuality(items);
            }

            return null;
        }

        private ItemQuality[] ParseItemQuality(List<string> quality)
        {
            return quality.Select(q =>
            {
                ItemQuality parsedQuality;
                var success = Enum.TryParse(q.ToUpper(), true, out parsedQuality);
                return new { success, parsedQuality };
            }).Where(x => x.success).Select(x => x.parsedQuality).ToArray();
        }

        public void WriteYaml(IEmitter emitter, object value, Type type)
        {
            throw new NotImplementedException();
        }
    }

    internal sealed class ItemTierYamlTypeConverter : IYamlTypeConverter
    {
        public bool Accepts(Type type)
        {
            return type == typeof(ItemTier[]);
        }

        public object ReadYaml(IParser parser, Type type)
        {
            if (parser.TryConsume<Scalar>(out var scalar))
            {
                var items = new List<string>() { scalar.Value };
                return ParseItemTier(items);
            }

            if (parser.TryConsume<SequenceStart>(out var _))
            {
                var items = new List<string>();
                while (parser.TryConsume<Scalar>(out var scalarItem))
                {
                    items.Add(scalarItem.Value);
                }

                parser.Consume<SequenceEnd>();
                return ParseItemTier(items);
            }

            return null;
        }

        private ItemTier[] ParseItemTier(List<string> quality)
        {
            return quality.Select(q =>
            {
                ItemTier parsedQuality;
                var success = Enum.TryParse(q.ToUpper(), true, out parsedQuality);
                return new { success, parsedQuality };
            }).Where(x => x.success).Select(x => x.parsedQuality).ToArray();
        }

        public void WriteYaml(IEmitter emitter, object value, Type type)
        {
            throw new NotImplementedException();
        }
    }

    internal sealed class SkillTreeYamlTypeConverter : IYamlTypeConverter
    {
        public bool Accepts(Type type)
        {
            return type == typeof(SkillTree);
        }

        public object ReadYaml(IParser parser, Type type)
        {
            if (parser.TryConsume<Scalar>(out var scalar))
            {
                if (Enum.TryParse(scalar.Value.Replace(" ", ""), true, out SkillTree skillTree))
                {
                    return skillTree;
                }
                else
                {
                    throw new Exception($"Failed to parse class tab: {scalar.Value}");
                }
            }

            return null;
        }

        public void WriteYaml(IEmitter emitter, object value, Type type)
        {
            throw new NotImplementedException();
        }
    }

    internal sealed class SkillsYamlTypeConverter : IYamlTypeConverter
    {
        public bool Accepts(Type type)
        {
            return type == typeof(Skill);
        }

        public object ReadYaml(IParser parser, Type type)
        {
            if (parser.TryConsume<Scalar>(out var scalar))
            {
                if (Enum.TryParse(scalar.Value.Replace(" ", ""), true, out Skill skill))
                {
                    return skill;
                }
                else
                {
                    throw new Exception($"Failed to parse skill: {scalar.Value}");
                }
            }

            return null;
        }

        public void WriteYaml(IEmitter emitter, object value, Type type)
        {
            throw new NotImplementedException();
        }
    }

    internal static class Helpers
    {
        internal static void WriteIconRendering(IEmitter emitter, IconRendering node)
        {
            var isFilled = node.IconColor != null && node.IconColor.A > 0;
            var isOutline = node.IconOutlineColor != null && node.IconOutlineColor.A > 0;

            if (isFilled)
            {
                emitter.Emit(new Scalar(null, "IconColor"));
                emitter.Emit(new Scalar(null, Helpers.GetColorName(node.IconColor)));
            }

            if (isOutline)
            {
                emitter.Emit(new Scalar(null, "IconOutlineColor"));
                emitter.Emit(new Scalar(null, Helpers.GetColorName(node.IconOutlineColor)));
            }

            if (isFilled || isOutline)
            {
                emitter.Emit(new Scalar(null, "IconShape"));
                emitter.Emit(new Scalar(null, node.IconShape.ToString()));
                emitter.Emit(new Scalar(null, "IconSize"));
                emitter.Emit(new Scalar(null, node.IconSize.ToString()));
            }

            if (isOutline)
            {
                emitter.Emit(new Scalar(null, "IconThickness"));
                emitter.Emit(new Scalar(null, node.IconThickness.ToString()));
            }
        }

        internal static void WritePOIRendering(IEmitter emitter, PointOfInterestRendering node)
        {
            WriteIconRendering(emitter, node);

            var hasLine = node.LineColor != null && node.LineColor.A > 0 && node.LineThickness > 0;
            var hasLabelColor = node.LabelColor != null && node.LabelColor.A > 0;
            var hasLabel = node.LabelFontSize > 0;

            if (hasLine)
            {
                emitter.Emit(new Scalar(null, "LineColor"));
                emitter.Emit(new Scalar(null, Helpers.GetColorName(node.LineColor)));
                emitter.Emit(new Scalar(null, "LineThickness"));
                emitter.Emit(new Scalar(null, node.LineThickness.ToString()));
                emitter.Emit(new Scalar(null, "ArrowHeadSize"));
                emitter.Emit(new Scalar(null, node.ArrowHeadSize.ToString()));
            }

            if (hasLabelColor)
            {
                emitter.Emit(new Scalar(null, "LabelColor"));
                emitter.Emit(new Scalar(null, Helpers.GetColorName(node.LabelColor)));
            }

            if (hasLabel)
            {
                emitter.Emit(new Scalar(null, "LabelFontSize"));
                emitter.Emit(new Scalar(null, node.LabelFontSize.ToString()));
                emitter.Emit(new Scalar(null, "LabelFont"));
                emitter.Emit(new Scalar(null, node.LabelFont.ToString()));
                emitter.Emit(new Scalar(null, "LabelTextShadow"));
                emitter.Emit(new Scalar(null, node.LabelTextShadow.ToString().ToLower()));
            }
        }

        internal static string GetColorName(Color color)
        {
            if (color.IsNamedColor)
            {
                return color.Name;
            }
            else
            {
                return color.R + ", " + color.G + ", " + color.B;
            }
        }
    }
}
