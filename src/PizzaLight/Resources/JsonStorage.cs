using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Noobot.Core.MessagingPipeline.Request;
using Serilog;

namespace PizzaLight.Resources
{
    public class JsonStorage : IMustBeInitialized
    {
        private readonly ILogger _log;
        private string _directory;

        public JsonStorage(ILogger log)
        {
            _log = log;
        }

        public Task Start()
        {
            _directory = Path.Combine(Environment.CurrentDirectory, "data");
            if (!Directory.Exists(_directory))
            {
                Directory.CreateDirectory(_directory);
            }
            return Task.CompletedTask;
        }

        public  Task Stop()
        {
            return Task.CompletedTask;
        }

        public async Task HandleMessage(IncomingMessage incomingMessage)
        {
        }

        public T[] ReadFile<T>(string fileName) where T : class, new()
        {
            string filePath = GetFilePath(fileName);
            if (!File.Exists(filePath))
            {
                File.Create(filePath).Dispose();
            }

            T[] result = new T[0];

            try
            {
                string file = File.ReadAllText(filePath);

                if (!string.IsNullOrEmpty(file))
                {
                    result = JsonConvert.DeserializeObject<T[]>(file);
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Error loading file '{filePath}' - {ex}");
            }

            return result;
        }

        public void SaveFile<T>(string fileName, T[] objects) where T : class, new()
        {
            string filePath = GetFilePath(fileName);
            File.WriteAllText(filePath, JsonConvert.SerializeObject(objects, Formatting.Indented));
        }

        public void DeleteFile(string fileName)
        {
            string filePath = GetFilePath(fileName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        private string GetFilePath(string fileName)
        {
            return Path.Combine(_directory, fileName + ".json");
        }
    }
}