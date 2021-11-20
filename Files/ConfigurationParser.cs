using System.Text;
using System;
using System.IO;
using Newtonsoft.Json;
using System.Diagnostics;

namespace MapAssist.Files
{
    public class ConfigurationParser<T>
    {
        public T ParseConfiguration()
        {
            var fileName = "";
            
            foreach(var fileNameInDirectory in System.IO.Directory.EnumerateFiles(System.IO.Directory.GetCurrentDirectory())) 
            {
                if(fileNameInDirectory.Contains("json"))
                {
                    fileName = fileNameInDirectory;
                    Debug.WriteLine($"Configuration file {fileNameInDirectory} found!");
                    break;
                }
            }

            if(fileName.Length < 1)
            {
                throw new Exception("Configuration JSON file not found!");
            }

            var fileManager = new FileManager(fileName);
            var JSONString = fileManager.ReadFile();
            if (JSONString.Length <= 0)
            {
                Debug.WriteLine($"File for {fileManager.GetAbsolutePath()} was empty ... Failed to parse configuration");
                return default(T);
            }
            T content = JsonConvert.DeserializeObject<T>(JSONString);
            return content;
        }

        public void SerializeToFile(T unserializedConfiguration)
        {
            var fileName = "MapAssistConfiguration.json";
            var serialized = JsonConvert.SerializeObject(unserializedConfiguration,Formatting.Indented);
            var fileManager = new FileManager(fileName);
            fileManager.WriteFile(serialized);
        }
    }
}
