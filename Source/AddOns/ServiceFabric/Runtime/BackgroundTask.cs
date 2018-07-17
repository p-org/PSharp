namespace Microsoft.PSharp.ServiceFabric
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public abstract class BackgroundTask
    {
        private CancellationToken taskToken;
        private Task currentTask;

        protected BackgroundTask()
        {
            this.currentTask = null;
        }

        protected virtual void LogSuccessfulRun(long runTime, bool isEnabled)
        {
        }

        protected virtual void LogFailedRun(Exception ex, long runTime, bool isEnabled)
        {
        }

        protected abstract Task Run(CancellationToken token);

        protected abstract TimeSpan WaitTime();

        private async Task RunInternal(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    DateTime beforeTime = DateTime.UtcNow;
                    bool isEnabled = this.IsEnabled();
                    if (isEnabled)
                    {
                        try
                        {
                            await this.Run(token);
                            this.LogSuccessfulRun((long)DateTime.UtcNow.Subtract(beforeTime).TotalMilliseconds, true);
                        }
                        catch (Exception ex)
                        {
                            this.LogFailedRun(ex, (long)DateTime.UtcNow.Subtract(beforeTime).TotalMilliseconds, true);
                        }

                        await this.Delay(token);
                    }
                    else
                    {
                        this.LogSuccessfulRun((long)DateTime.UtcNow.Subtract(beforeTime).TotalMilliseconds, false);
                        // If disabled, check for the status again in a minute
                        await Task.Delay(TimeSpan.FromMinutes(1), token);
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        protected virtual Task Delay(CancellationToken token)
        {
            return Task.Delay(this.WaitTime(), token);
        }


        public Task Start(CancellationToken token)
        {
            if (this.currentTask != null && !this.currentTask.IsCompleted && !this.taskToken.IsCancellationRequested)
            {
                throw new InvalidOperationException("Cannot start a task which is already running! Please cancel the existing task using the token");
            }

            this.taskToken = token;
            this.currentTask = this.RunInternal(token);
            return this.currentTask;
        }

        protected abstract bool IsEnabled();
    }
}