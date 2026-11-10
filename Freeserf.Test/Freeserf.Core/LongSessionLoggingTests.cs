using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Freeserf.Test.Freeserf.Core
{
    [TestClass]
    [DoNotParallelize]
    public class LongSessionLoggingTests
    {
        Stream previousStream;
        long? previousLimit;
        Log.Level previousLevel;
        static Queue<int> TrackedLines => (Queue<int>)typeof(Log)
            .GetField("lineSizes", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);

        [TestInitialize]
        public void RememberLogSettings()
        {
            previousStream = (Stream)typeof(Log).GetField("stream", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            previousLimit = Log.MaxSize;
            previousLevel = Log.LogLevel;
            Log.SetLevel(Log.Level.Info);
        }

        [TestCleanup]
        public void RestoreLogSettings()
        {
            Log.SetStream(previousStream);
            Log.MaxSize = previousLimit;
            Log.SetLevel(previousLevel);
        }

        [TestMethod]
        public void UnlimitedConsoleLogging_ShouldNotAccumulateLineHistory()
        {
            using var stream = new MemoryStream();
            Log.SetStream(stream);
            Log.MaxSize = null;
            for (int i = 0; i < 10000; ++i)
                Log.Info.Write(ErrorSystemType.Network, "heartbeat");
            Assert.AreEqual(0, TrackedLines.Count);
            Assert.IsTrue(stream.Length > 0);
        }

        [TestMethod]
        public void LimitedFileLogging_ShouldTrimWholeLinesInBatches()
        {
            string path = Path.GetTempFileName();
            try
            {
                using var stream = new LogFileStream(path);
                Log.SetStream(stream);
                Log.MaxSize = 4096;
                long previousLength = 0;
                int trims = 0;
                for (int i = 0; i < 500; ++i)
                {
                    Log.Info.Write(ErrorSystemType.Network, $"packet {i:D5}");
                    // Windows checks sharing in both directions: this reader
                    // must allow the logger's already-open write handle too.
                    string text;
                    using (var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var reader = new StreamReader(input))
                        text = reader.ReadToEnd();
                    Assert.IsTrue(text.Length <= Log.MaxSize.Value);
                    Assert.IsTrue(text.EndsWith($"packet {i:D5}" + Environment.NewLine));
                    Assert.IsTrue(text.Split(Environment.NewLine)[0].Contains("Info: [Network] packet "));
                    if (text.Length < previousLength)
                    {
                        ++trims;
                        Assert.IsTrue(previousLength - text.Length > 512, "Trimming should free substantial space for later writes.");
                    }
                    previousLength = text.Length;
                }
                Assert.IsTrue(trims > 0 && trims < 50, "Compaction must not happen for each new line.");
            }
            finally
            {
                File.Delete(path);
            }
        }

        [TestMethod]
        public void SwitchingStreams_ShouldDiscardOldSizeAccounting()
        {
            using var first = new MemoryStream();
            using var second = new MemoryStream();
            Log.SetStream(first);
            Log.MaxSize = null;
            Log.Info.Write(ErrorSystemType.Network, "first stream");
            Log.SetStream(second);
            Assert.AreEqual(0, TrackedLines.Count);
            Parallel.For(0, 1000, i =>
            {
                if (i % 2 == 0) Log.Info.Write(ErrorSystemType.Network, "info");
                else Log.Warn.Write(ErrorSystemType.Network, "warning");
            });
            second.Position = 0;
            using var reader = new StreamReader(second);
            Assert.AreEqual(1000, reader.ReadToEnd().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Length);
            Assert.AreEqual(0, TrackedLines.Count);
        }
    }
}
