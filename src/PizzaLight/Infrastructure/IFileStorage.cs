namespace PizzaLight.Infrastructure
{
    public interface IFileStorage : IMustBeInitialized
    {
        void DeleteFile(string fileName);
        T[] ReadFile<T>(string fileName) where T : class, new();
        void SaveFile<T>(string fileName, T[] objects) where T : class, new();
    }
}