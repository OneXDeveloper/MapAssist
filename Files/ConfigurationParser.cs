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
        public static T ParseConfiguration()
        {
            var fileName = $"./{System.AppDomain.CurrentDomain.FriendlyName}.yaml";
            
            var fileManager = new FileManager(fileName);

            if(!fileManager.FileExists())
            {
                throw new Exception($"{fileName} needs to be present on the same level as the executable");
            }

            var YamlString = fileManager.ReadFile();
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(PascalCaseNamingConvention.Instance)
                .WithTypeConverter(new AreaArrayYamlTypeConverter())
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
