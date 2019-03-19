using System.Threading.Tasks;

namespace PizzaLight.Infrastructure
{
    public interface IMustBeInitialized
    {
        Task Start();
        Task Stop();

    }
}