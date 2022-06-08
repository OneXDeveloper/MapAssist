using MapAssist.Helpers;
using MapAssist.Settings;
using MapAssist.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace MapAssist.Files
{
    public class ConfigurationParser<T>
    {
        private static readonly NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();

        private IDeserializer deserializer;

        public ConfigurationParser()
        {
            var builder = new DeserializerBuilder();

            if (typeof(T) == typeof(MapAssistConfiguration))
            {
                builder.IgnoreUnmatchedProperties();
            }

            deserializer = builder
                .WithNamingConvention(PascalCaseNamingConvention.Instance)
                .WithTypeConverter(new AreaArrayYamlTypeConverter())
                .WithTypeConverter(new ItemYamlTypeConverter())
                .WithTypeConverter(new ItemQualityYamlTypeConverter())
                .WithTypeConverter(new ItemTierYamlTypeConverter())
                .WithTypeConverter(new SkillTreeYamlTypeConverter())
                .WithTypeConverter(new SkillsYamlTypeConverter())
                .Build();
        }

        public T ParseConfigurationFile(string fileName)
        {
            var fileManager = new FileManager(fileName);

            if (!fileManager.FileExists())
            {
                throw new Exception($"{fileName} needs to be present on the same level as the executable");
            }

            var YamlString = fileManager.ReadFile();
            CheckDuplicateKeys(YamlString);

            var configuration = deserializer.Deserialize<T>(YamlString);
            return configuration;
        }

        public void CheckDuplicateKeys(string YamlString)
        {
            var seen = new List<Item>();
            foreach (var (textRaw, line) in YamlString.Split(new char[] { '\n', '\r' }, StringSplitOptions.None).Select((text, line) => (text, line)))
            {
                if (textRaw.Length == 0) continue;

                var text = textRaw.Substring(0, textRaw.Length - 1);

                var item = Items.ParseFromString(text);
                if (item == null) continue;

                if (!seen.Contains((Item)item))
                {
                    seen.Add((Item)item);
                }
                else
                {
                    throw new Exception($"Duplicate {text} entry on line {line + 1}");
                }
            }
        }

        /**
         * Parses the dual configuration setup so that the custom configuration can override individual values in the
         * default configuration. Note that for fields like the MapConfiguration.Mapcolors, the entire value will
         * be overwritten.
         *
         * The approach here is to first deserialize the yaml configs into Dictionary<object, object>, then perform
         * a recursive merge on a field by field basis. The result of this merging is then serialized back to yaml,
         * then deserialized again into our MapAssistConfiguration POCO.
         */

        public MapAssistConfiguration ParseConfigurationMain(byte[] resourcePrimary, string fileNameOverride = null)
        {
            var yamlPrimary = Encoding.Default.GetString(resourcePrimary);

            if (fileNameOverride == null)
            {
                return deserializer.Deserialize<MapAssistConfiguration>(yamlPrimary);
            }

            var fileManagerOverride = new FileManager(fileNameOverride);
            if (!fileManagerOverride.FileExists())
            {
                return deserializer.Deserialize<MapAssistConfiguration>(yamlPrimary);
            }

            var yamlOverride = fileManagerOverride.ReadFile();

            var primaryConfig = deserializer.Deserialize<Dictionary<object, object>>(yamlPrimary);
            var overrideConfig = deserializer.Deserialize<Dictionary<object, object>>(yamlOverride);

            // Have to check here for a null customized config - either a blank or fully commented out yaml file will result in a null dict
            // from the deserializer.
            if (overrideConfig != null)
            {
                Merge(primaryConfig, overrideConfig);
            }

            var serializer = new SerializerBuilder().Build();
            var yaml = serializer.Serialize(primaryConfig);
            var configuration = deserializer.Deserialize<MapAssistConfiguration>(yaml);
            return configuration;
        }

        public void Merge(Dictionary<object, object> primary, Dictionary<object, object> secondary)
        {
            foreach (var tuple in secondary)
            {
                if (!primary.ContainsKey(tuple.Key))
                {
                    primary.Add(tuple.Key, tuple.Value);
                    continue;
                }

                var primaryValue = primary[tuple.Key];
                if (!(primaryValue is IDictionary))
                {
                    primary[tuple.Key] = tuple.Value;
                    continue;
                }
                else
                {
                    if (tuple.Key.ToString() == "MapConfiguration") // Don't merge this object, simply take the user provided one
                    {
                        primary[tuple.Key] = secondary[tuple.Key];
                        continue;
                    }

                    /**
                     * Don't allow an override to try and use a null dict
                     * This allows things like below to exist in overrides without breaking anything
                     * MapColorConfiguration:
                        MapColors:
                        # '0': '50, 50, 50'
                        # '2': '10, 51, 23'
                        ...
                    */
                    if (secondary[tuple.Key] != null)
                    {
                        Merge((Dictionary<object, object>)primaryValue, (Dictionary<object, object>)secondary[tuple.Key]);
                    }
                }
            }
        }

        public void SerializeToFile(T unserializedConfiguration)
        {
            var config = MapAssistConfiguration.Loaded;
            using (var streamWriter = new StreamWriter("Config.yaml"))
            {
                streamWriter.WriteLine("# Change these settings from the Config GUI provided in MapAssist");
                var serializer = new SerializerBuilder()
                    .WithNamingConvention(PascalCaseNamingConvention.Instance)
                    .WithTypeConverter(new FloatPrecisionConverter())
                    .WithTypeConverter(new AreaArrayYamlTypeConverter())
                    .WithTypeConverter(new IconRenderingTypeConverter())
                    .WithTypeConverter(new PointOfInterestRenderingTypeConverter())
                    .WithTypeConverter(new PortalRenderingTypeConverter())
                    .WithTypeConverter(new MapColorConfigurationTypeConverter())
                    .WithTypeConverter(new ColorConfigurationTypeConverter())
                    .Build();
                serializer.Serialize(streamWriter, config);
            }
        }
    }
}
