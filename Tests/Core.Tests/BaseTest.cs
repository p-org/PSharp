// ------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.PSharp.IO;
using Microsoft.PSharp.Tests.Common;
using Xunit;
using Xunit.Abstractions;

using Common = Microsoft.PSharp.Tests.Common;

namespace Microsoft.PSharp.Core.Tests
{
    public abstract class BaseTest : Common.BaseTest
    {
        public BaseTest(ITestOutputHelper output)
            : base(output)
        {
        }

        protected void Run(Action<IMachineRuntime> test, Configuration configuration = null)
        {
            configuration = configuration ?? GetConfiguration();

            ILogger logger;
            if (configuration.IsVerbose)
            {
                logger = new TestOutputLogger(this.TestOutput, true);
            }
            else
            {
                logger = new DisposingLogger();
            }

            try
            {
                var runtime = PSharpRuntime.Create(configuration);
                runtime.SetLogger(logger);
                test(runtime);
            }
            catch (Exception ex)
            {
                Assert.False(true, ex.Message + "\n" + ex.StackTrace);
            }
            finally
            {
                logger.Dispose();
            }
        }

        protected async Task RunAsync(Func<IMachineRuntime, Task> test, Configuration configuration = null)
        {
            configuration = configuration ?? GetConfiguration();

            ILogger logger;
            if (configuration.IsVerbose)
            {
                logger = new TestOutputLogger(this.TestOutput, true);
            }
            else
            {
                logger = new DisposingLogger();
            }

            try
            {
                var runtime = PSharpRuntime.Create(configuration);
                runtime.SetLogger(logger);
                await test(runtime);
            }
            catch (Exception ex)
            {
                Assert.False(true, ex.Message + "\n" + ex.StackTrace);
            }
            finally
            {
                logger.Dispose();
            }
        }

        protected static async Task WaitAsync(Task task, int millisecondsDelay = 5000)
        {
            await Task.WhenAny(task, Task.Delay(millisecondsDelay));
            Assert.True(task.IsCompleted);
        }

        protected static async Task<TResult> GetResultAsync<TResult>(Task<TResult> task, int millisecondsDelay = 5000)
        {
            await Task.WhenAny(task, Task.Delay(millisecondsDelay));
            Assert.True(task.IsCompleted);
            return await task;
        }

        protected static Configuration GetConfiguration()
        {
            return Configuration.Create();
        }
    }
}
