using System.Threading.Tasks;

namespace AzureFunctions.SingletonTaskDrainer
{
    public interface ITaskDrainer
    {
        Task QueueWork(InputObject input);
    }
}
