using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Serilog;

namespace PizzaLight.Infrastructure
{
    public class JsonStorage : IFileStorage
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

        public T[] ReadArray<T>(string fileName) where T : class, new()
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
                throw;
            }

            return result;
        }

        public void SaveArray<T>(string fileName, T[] objects) where T : class, new()
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


        public T ReadObject<T>(string fileName) where T : class, new()
        {
            string filePath = GetFilePath(fileName);
            if (!File.Exists(filePath))
            {
                File.Create(filePath).Dispose();
            }

            try
            {
                string file = File.ReadAllText(filePath);

                if (!string.IsNullOrEmpty(file))
                {
                    return JsonConvert.DeserializeObject<T>(file);
                }
                else
                {
                    return (T)null;
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Error loading file '{filePath}' - {ex}");
                throw;           
            }
        }

        public void SaveObject<T>(string fileName, T obj) where T : class, new()
        {
            string filePath = GetFilePath(fileName);
            File.WriteAllText(filePath, JsonConvert.SerializeObject(obj, Formatting.Indented));
        }

        private string GetFilePath(string fileName)
        {
            if (_directory is null)
            { throw new NotStartedException("Storage not properly started with Start()"); }
            return Path.Combine(_directory, fileName + ".json");
        }

    }

    public interface IFileStorage : IMustBeInitialized
    {
        void DeleteFile(string fileName);
        T[] ReadArray<T>(string fileName) where T : class, new();
        void SaveArray<T>(string fileName, T[] objects) where T : class, new();
        T ReadObject<T>(string fileName) where T : class, new();
        void SaveObject<T>(string fileName, T objects) where T : class, new();

    }

}