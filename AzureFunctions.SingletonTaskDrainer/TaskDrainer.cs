using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AzureFunctions.SingletonTaskDrainer
{
    public sealed class TaskDrainer : ITaskDrainer, IAsyncDisposable
    {
        private const int MaxItems = 10; // TODO: Configurable/injectable
        private readonly TimeSpan _timeToWaitBetweenBatches = TimeSpan.FromSeconds(2); // TODO: Configurable/injectable
        private readonly Timer _flushTimer;

        private readonly ILogger _log;
        private readonly ConcurrentBag<InputObject> _workItems = new ConcurrentBag<InputObject>();

        public TaskDrainer(ILogger<TaskDrainer> log)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
            this._flushTimer = new Timer(TimerCallback, null, TimeSpan.Zero, _timeToWaitBetweenBatches);
        }

        public async Task QueueWork(InputObject input)
        {
            if (_workItems.Count >= MaxItems)
            {
                _log.LogInformation("Performing manual flush (max items reached).");
                _flushTimer.Change(Timeout.Infinite, Timeout.Infinite); // "Pause" timer
                await this.FlushAsync();
            }

            // Reset our timer's next poll
            _flushTimer.Change(_timeToWaitBetweenBatches, _timeToWaitBetweenBatches);

            _workItems.Add(input);
        }

        /// <see href="https://github.com/davidfowl/AspNetCoreDiagnosticScenarios/blob/master/AsyncGuidance.md#timer-callbacks"/>
        private void TimerCallback(object state)
        {
            this._log.LogInformation("Performing automatic flush (timer callback called).");

            _ = FlushAsync();
        }

        private async Task FlushAsync()
        {
            IEnumerable<Task> tasksToExecute;

            lock (_workItems)
            {
                tasksToExecute = _workItems.Select(CreateWorkTask); // Simulate work to do

                if (_workItems.IsEmpty)
                {
                    _log.LogInformation("Flush requested on empty collection (no work to do).");
                }
                else
                {
                    _log.LogInformation("Flush requested for items with calling threads: " +
                        $"[{string.Join(',', _workItems.Select(wi => wi.CallerThreadId))}]");
                }

                _workItems.Clear();
            }

            await Task.WhenAll(tasksToExecute);

            _log.LogInformation("Flush completed.");
        }

        private Task CreateWorkTask(object input)
        {
            // How long each individual task will take to complete on its own
            var simulatedIndividualTaskWaitTime = TimeSpan.FromSeconds(2);

            return Task.Delay(simulatedIndividualTaskWaitTime);
        }

        public async ValueTask DisposeAsync()
        {
            // TODO: Test Function App shutdown. All requests should be completed before the application shuts down.
            _log.LogInformation("Singleton was requested to be disposed.");

            await this._flushTimer.DisposeAsync();
            await this.FlushAsync();
        }
    }
}
