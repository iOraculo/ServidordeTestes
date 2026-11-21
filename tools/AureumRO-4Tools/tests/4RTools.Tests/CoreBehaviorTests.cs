using System;
using System.Diagnostics;
using System.Threading;
using _4RTools.Model;
using _4RTools.Utils;
using Xunit;

namespace _4RTools.Tests
{
    public class CoreBehaviorTests
    {
        [Theory]
        [InlineData(50u, 100u, 60, true)]
        [InlineData(60u, 100u, 60, false)]
        [InlineData(0u, 0u, 80, false)]
        [InlineData(50u, 100u, 0, false)]
        [InlineData(100000000u, 200000000u, 51, true)]
        [InlineData(100000000u, 200000000u, 50, false)]
        public void Client_IsBelowPercent_HandlesEdgeCasesAndLargeValuesWithoutOverflow(uint current, uint max, int percent, bool expected)
        {
            Assert.Equal(expected, Client.IsBelowPercent(current, max, percent));
        }

        [Fact]
        public void _4RThread_StopsCooperativelyAndInterruptsLongSleepImmediately()
        {
            int iterations = 0;
            ManualResetEventSlim started = new ManualResetEventSlim(false);

            _4RThread worker = new _4RThread(token =>
            {
                Interlocked.Increment(ref iterations);
                started.Set();
                _4RThread.Sleep(30000, token);
            }, "TestWorker");

            _4RThread.Start(worker);
            Assert.True(started.Wait(TimeSpan.FromSeconds(2)));
            Assert.True(worker.IsRunning);

            Stopwatch sw = Stopwatch.StartNew();
            _4RThread.Stop(worker);
            sw.Stop();

            Assert.False(worker.IsRunning);
            Assert.True(sw.ElapsedMilliseconds < 2000, "Stop should interrupt Sleep promptly without waiting 30s.");
            Assert.True(iterations >= 1);

            // Idempotent stop calls should not throw.
            _4RThread.Stop(worker);
            _4RThread.Stop(null);
            started.Dispose();
        }
    }
}
