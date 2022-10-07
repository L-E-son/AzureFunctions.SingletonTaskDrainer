using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace AzureFunctions.SingletonTaskDrainer
{
    public class SingletonCallerTrigger
    {
        private const string NCronTabExpression = "*/1 * * * * *";

        private readonly ILogger _log;
        private readonly ITaskDrainer _taskDrainer;
        private readonly Random _random = new Random();

        public SingletonCallerTrigger(
            ILogger<SingletonCallerTrigger> log,
            ITaskDrainer taskDrainer)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _taskDrainer = taskDrainer ?? throw new ArgumentNullException(nameof(taskDrainer));
        }

        [FunctionName(nameof(IWillTryAndUseTheSingleton))]
        public async Task IWillTryAndUseTheSingleton(
            [TimerTrigger(NCronTabExpression)] TimerInfo myTimer)
        {
            var randomSecondsToWait = _random.Next(0, 2);

            await Task.Delay(TimeSpan.FromSeconds(randomSecondsToWait));

            var threadId = Thread.CurrentThread.ManagedThreadId;

            _log.LogDebug($"Thread with ID {threadId} created.");

            var myInput = new InputObject { CallerThreadId = threadId };

            _log.LogInformation($"Calling singleton from thread {threadId}");

            await _taskDrainer.QueueWork(myInput);

            _log.LogDebug($"Thread {threadId} finished calling singleton. Thread will now be reclaimed.");

            // Thread reclaimed by thread pool after method exits
        }
    }
}
