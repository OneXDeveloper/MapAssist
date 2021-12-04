using System;
using System.Collections.Generic;
using System.Linq;
using MapAssist.Types;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace MapAssist.Helpers
{
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
            throw new NotImplementedException();
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
            return Enum.GetValues(typeof(Area)).Cast<Area>().FirstOrDefault(area => area.Name() == name);
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
                var items = new List<string>() {scalar.Value};
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
                var success = Enum.TryParse(q.ToUpper(), out parsedQuality);
                return new {success, parsedQuality};
            }).Where(x => x.success).Select(x => x.parsedQuality).ToArray();
        }

        public void WriteYaml(IEmitter emitter, object value, Type type)
        {
            throw new NotImplementedException();
        }
    }
}
