using System.Text;
using System;
using System.IO;
using Newtonsoft.Json;
using System.Diagnostics;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using MapAssist.Helpers;

namespace MapAssist.Files
{
    public class ConfigurationParser<T>
    {
        public static T ParseConfiguration(string fileName)
        {
            var fileManager = new FileManager(fileName);

            if(!fileManager.FileExists())
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

        public void SerializeToFile(T unserializedConfiguration)
        {
            throw new System.NotImplementedException();
        }
    }
}
