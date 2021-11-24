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
            if (type == typeof(Area[]))
                return true;
            return false;
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

        public Area[] ParseAreaStringList(List<string> areas)
        {
            return areas
                .Select(o => LookupAreaByName(o.Trim()))
                .Where(o => o != Area.None)
                .ToArray();
        }

        public Area LookupAreaByName(string name)
        {
            return Enum.GetValues(typeof(Area)).Cast<Area>().FirstOrDefault(area => area.Name() == name);
        }
    }
}
