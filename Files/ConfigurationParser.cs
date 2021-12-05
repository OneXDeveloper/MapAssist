using System.Text;
using System;
using System.IO;
using Newtonsoft.Json;
using System.Diagnostics;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using MapAssist.Helpers;
using MapAssist.Settings;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

namespace MapAssist.Files
{
    public class ConfigurationParser<T>
    {
        public static T ParseConfigurationFile(string fileName)
        {
            var fileManager = new FileManager(fileName);

            if (!fileManager.FileExists())
            {
                throw new Exception($"{fileName} needs to be present on the same level as the executable");
            }

            var YamlString = fileManager.ReadFile();
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(PascalCaseNamingConvention.Instance)
                .WithTypeConverter(new AreaArrayYamlTypeConverter())
                .WithTypeConverter(new ItemQualityYamlTypeConverter())
                .Build();
            var configuration = deserializer.Deserialize<T>(YamlString);
            return configuration;
        }

        public static MapAssistConfiguration ParseConfigurationMain(byte[] resourcePrimary, string fileNameOverride)
        {
            var yamlPrimary = Encoding.Default.GetString(resourcePrimary);

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(PascalCaseNamingConvention.Instance)
                .WithTypeConverter(new AreaArrayYamlTypeConverter())
                .WithTypeConverter(new ItemQualityYamlTypeConverter())
                .Build();

            var fileManagerOverride = new FileManager(fileNameOverride);
            if (!fileManagerOverride.FileExists())
            {
                return deserializer.Deserialize<MapAssistConfiguration>(yamlPrimary);
            }

            var yamlOverride = fileManagerOverride.ReadFile();

            var testPrimary = deserializer.Deserialize<Dictionary<object, object>>(yamlPrimary);
            var testOverride = deserializer.Deserialize<Dictionary<object, object>>(yamlOverride);
            //var configuration = deserializer.Deserialize<T>(YamlString);
            Merge(testPrimary, testOverride);
            var serializer = new SerializerBuilder().Build();
            var yaml = serializer.Serialize(testPrimary);
            var configuration = deserializer.Deserialize<MapAssistConfiguration>(yaml);
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
                    Merge((Dictionary<object, object>)primaryValue, (Dictionary<object, object>)secondary[tuple.Key]);
                }
            }
        }

        public void SerializeToFile(T unserializedConfiguration)
        {
            throw new System.NotImplementedException();
        }
    }
}
