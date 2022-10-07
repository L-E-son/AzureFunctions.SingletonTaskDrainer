using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(AzureFunctions.SingletonTaskDrainer.Startup))]
namespace AzureFunctions.SingletonTaskDrainer
{
    internal class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddSingleton<ITaskDrainer, TaskDrainer>();
        }
    }
}
