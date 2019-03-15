using System.Threading.Tasks;

namespace PizzaLight.Resources
{
    public interface IMustBeInitialized
    {
        Task Start();
        Task Stop();

    }
}