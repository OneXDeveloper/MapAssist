using System.Text;
using System;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using MapAssist.Helpers;
using MapAssist.Settings;
using System.Collections.Generic;
using System.Collections;
using YamlDotNet.Core;
using System.Linq;
using System.Text.RegularExpressions;

namespace MapAssist.Files
{
    public class ConfigurationParser<T>
    {
        private static readonly NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();

        private static IDeserializer deserializer = new DeserializerBuilder()
                .WithNamingConvention(PascalCaseNamingConvention.Instance)
                .WithTypeConverter(new AreaArrayYamlTypeConverter())
                .WithTypeConverter(new ItemQualityYamlTypeConverter())
                .Build();

        public static T ParseConfigurationFile(string fileName)
        {
            var fileManager = new FileManager(fileName);

            if (!fileManager.FileExists())
            {
                throw new Exception($"{fileName} needs to be present on the same level as the executable");
            }

            var YamlString = fileManager.ReadFile();

            var configuration = TryDeserialize<T>(YamlString);
            return configuration;
        }

        /**
         * Parses the dual configuration setup so that the custom configuration can override individual values in the
         * default configuration. Note that for fields like PrefetchAreas and the MapConfiguration.Mapcolors, 
         * the entire value will be overwritten.
         * 
         * The approach here is to first deserialize the yaml configs into Dictionary<object, object>, then perform
         * a recursive merge on a field by field basis. The result of this merging is then serialized back to yaml, 
         * then deserialized again into our MapAssistConfiguration POCO.
         */
        public static MapAssistConfiguration ParseConfigurationMain(byte[] resourcePrimary, string fileNameOverride)
        {
            var yamlPrimary = Encoding.Default.GetString(resourcePrimary);

            var fileManagerOverride = new FileManager(fileNameOverride);
            if (!fileManagerOverride.FileExists())
            {
                return TryDeserialize<MapAssistConfiguration>(yamlPrimary);
            }

            var yamlOverride = fileManagerOverride.ReadFile();

            var primaryConfig = TryDeserialize<Dictionary<object, object>>(yamlPrimary);
            var overrideConfig = TryDeserialize<Dictionary<object, object>>(yamlOverride);

            // Have to check here for a null customized config - either a blank or fully commented out yaml file will result in a null dict
            // from the deserializer.
            if (overrideConfig != null)
            {
                Merge(primaryConfig, overrideConfig);
            }

            var serializer = new SerializerBuilder().Build();
            var yaml = serializer.Serialize(primaryConfig);
            var configuration = TryDeserialize<MapAssistConfiguration>(yaml);
            return configuration;
        }

        public static void Merge(Dictionary<object, object> primary, Dictionary<object, object> secondary)
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
        
        public static U TryDeserialize<U>(string data)
        {
            try
            {
                return deserializer.Deserialize<U>(data);
            }
            catch (YamlException ex)
            {
                if (ex.Start.Line > 1 || ex.Start.Column > 1 || ex.Start.Index > 0)
                {
                    var lines = data.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

                    var numLinesToLog = 5;
                    var linesToLog = lines.Skip(Math.Max(ex.Start.Line - numLinesToLog + 1, 0)).Take(5).ToArray();

                    _log.Info("Invalid yaml block" + Environment.NewLine + string.Join(Environment.NewLine, linesToLog));
                }

                throw ex;
            }
        }

        public void SerializeToFile(T unserializedConfiguration)
        {
            throw new System.NotImplementedException();
        }
    }
}
