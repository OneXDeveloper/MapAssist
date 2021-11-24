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
            var fileName = "";
            
            foreach(var fileNameInDirectory in System.IO.Directory.EnumerateFiles(System.IO.Directory.GetCurrentDirectory())) 
            {
                if(fileNameInDirectory.Contains("yaml"))
                {
                    fileName = fileNameInDirectory;
                    Debug.WriteLine($"Configuration file {fileNameInDirectory} found!");
                    break;
                }
            }

            if(fileName.Length < 1)
            {
                throw new Exception("Configuration yaml file not found!");
            }

            var fileManager = new FileManager(fileName);

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
