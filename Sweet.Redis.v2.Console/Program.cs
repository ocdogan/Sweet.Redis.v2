using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace Sweet.Redis.v2
{
    class Program
    {
        #region TestResult

        private struct TestResult
        {
            public string TestName;
            public long LoopCount;
            public long FailCount;
            public double Concurrency;
            public long SumOfInnerTicks;
            public double SumOfInnerMSecs;
        }

        #endregion TestResult

        private const int RedisPort = 6380;

        static void Main(string[] args)
        {
            do
            {
                Console.Clear();

                // UnitTests();

                // PerformancePingTests();
                PerformanceGetTinyTests();
                // PerformancePipeTests();

                // MonitorTest1();
                // MonitorTest2();

                // PubSubTest1();
                // PubSubTest2();

                // MultiThreadingPipedTest1();
                // MultiThreadingPipedTest2();
                // MultiThreadingPipedTest3();
                // MultiThreadingPipedTest4();

                // MultiThreadingGetTest1();
                // MultiThreadingGetTest2();
                // MultiThreadingGetTest3();
                // MultiThreadingGetTest4();
                // MultiThreadingGetTest5();
                // MultiThreadingGetTest6();
            }
            while (Console.ReadKey(true).Key != ConsoleKey.Escape);
        }

        #region Tests

        #region Multi-Threading

        #region Get Tests

        static void MultiThreadingGetTest1()
        {
            MultiThreadingGetTest("MultiThreadingGetTest1", loopCount: 10, threadCount: 1, innerCount: 1000);
        }

        static void MultiThreadingGetTest2()
        {
            MultiThreadingGetTest("MultiThreadingGetTest2", loopCount: 2, threadCount: 5, innerCount: 1000);
        }

        static void MultiThreadingGetTest3()
        {
            MultiThreadingGetTest("MultiThreadingGetTest3", loopCount: 2, threadCount: 10, innerCount: 1000);
        }

        static void MultiThreadingGetTest4()
        {
            MultiThreadingGetTest("MultiThreadingGetTest4", loopCount: 2, threadCount: 50, innerCount: 1000);
        }

        static void MultiThreadingGetTest5()
        {
            MultiThreadingGetTest("MultiThreadingGetTest5", loopCount: 2, threadCount: 64, innerCount: 1000);
        }

        static void MultiThreadingGetTest6()
        {
            MultiThreadingGetTest("MultiThreadingGetTest6", loopCount: 2, threadCount: 10, innerCount: 2000);
        }

        static void MultiThreadingGetTest(string testName, int loopCount, int threadCount, int innerCount)
        {
            MultiThreadingGetTestBase(testName, loopCount, threadCount,
                (db) =>
                {
                    for (var i = 0; i < 10; i++)
                        db.Strings.Set("getTinyText" + i, Encoding.UTF8.GetBytes("xx" + i));
                },
                (db) =>
                {
                    var list = new List<RedisBytes>();
                    for (var i = 0; i < innerCount; i++)
                        list.Add(db.Strings.Get("getTinyText" + (i % 10)));

                    for (var i = 0; i < innerCount; i++)
                    {
                        RedisByteArray data1 = list[i].Value;
                        if (data1 != "xx" + (i % 10))
                            return false;
                    }

                    return true;
                }, innerCount);
        }

        static void MultiThreadingGetTestBase(string testName, int loopCount, int threadCount,
                                           Action<IRedisDb> setup, Func<IRedisDb, bool> func, int multiplier = 1)
        {
            threadCount = Math.Max(1, threadCount);

            using (var manager = new RedisAsyncManager(new RedisConnectionSettings("127.0.0.1", RedisPort, bulkSendFactor: 10000)))
            {
                var loopIndex = 1;
                List<Thread> thList = null;
                do
                {
                    Console.Clear();
                    Console.WriteLine();

                    if (setup != null)
                    {
                        using (var db = manager.GetDb(11))
                            setup(db);
                    }

                    var oldThList = thList;
                    if (oldThList != null)
                    {
                        for (var i = 0; i < oldThList.Count; i++)
                        {
                            var th = oldThList[i];
                            try
                            {
                                if (th.IsAlive)
                                    th.Interrupt();
                            }
                            catch (Exception)
                            { }
                        }
                    }

                    thList = new List<Thread>(threadCount);
                    var results = new Dictionary<string, TestResult>();

                    try
                    {
                        for (var i = 0; i < threadCount; i++)
                        {
                            var th = new Thread((obj) =>
                            {
                                var tupple = (Tuple<Thread, AutoResetEvent>)obj;

                                var @this = tupple.Item1;
                                var autoReset = tupple.Item2;

                                var ticks = 0L;
                                var failCount = 0;

                                var innerSw = new Stopwatch();
                                try
                                {
                                    using (var db = manager.GetDb(11))
                                    {
                                        for (var j = 0; j < loopCount; j++)
                                        {
                                            var num = j.ToString();

                                            innerSw.Restart();

                                            var ok = func(db);

                                            innerSw.Stop();

                                            ticks += innerSw.ElapsedTicks;

                                            if (!ok)
                                                failCount++;
                                        }
                                    }
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(e);
                                }
                                finally
                                {
                                    try
                                    {
                                        var elapsedMSecs = TimeSpan.FromTicks(ticks).TotalMilliseconds;
                                        var requestCount = loopCount * multiplier;

                                        var testResult = new TestResult
                                        {
                                            TestName = testName,
                                            LoopCount = requestCount,
                                            FailCount = failCount,
                                            SumOfInnerTicks = ticks,
                                            SumOfInnerMSecs = elapsedMSecs,
                                            Concurrency = Math.Ceiling(1000 * ((double)requestCount / elapsedMSecs))
                                        };

                                        lock (results)
                                        {
                                            results[@this.Name] = testResult;
                                        }
                                    }
                                    finally
                                    {
                                        autoReset.Set();
                                    }
                                }
                            });

                            th.Name = loopIndex++.ToString("D2") + "." + i.ToString("D2");
                            th.IsBackground = true;
                            thList.Add(th);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }

                    var autoResets = new AutoResetEvent[thList.Count];
                    try
                    {
                        for (var i = 0; i < thList.Count; i++)
                        {
                            var th = thList[i];

                            var autoReset = new AutoResetEvent(false);
                            autoResets[i] = autoReset;

                            th.Start(new Tuple<Thread, AutoResetEvent>(th, autoReset));
                        }
                    }
                    finally
                    {
                        WaitHandle.WaitAll(autoResets);
                    }

                    Console.Clear();

                    var sumOfResults = new TestResult { TestName = "Sum" };

                    foreach (var kv in results)
                    {
                        var result = kv.Value;

                        sumOfResults.LoopCount += result.LoopCount;
                        sumOfResults.FailCount = result.FailCount;
                        sumOfResults.SumOfInnerTicks = result.SumOfInnerTicks;
                        sumOfResults.SumOfInnerMSecs = result.SumOfInnerMSecs;

                        Console.WriteLine();
                        WriteResult(result);
                    }

                    sumOfResults.Concurrency = Math.Ceiling(1000 * ((double)sumOfResults.LoopCount / (sumOfResults.SumOfInnerTicks / TimeSpan.TicksPerMillisecond)));

                    Console.WriteLine();
                    WriteResult(sumOfResults);
                }
                while (Console.ReadKey(true).Key != ConsoleKey.Escape);
            }
        }


        #endregion Get Tests

        #region Piped Tests

        static void MultiThreadingPipedTest1()
        {
            MultiThreadingPipedTest("MultiThreadingPipedTest1", loopCount: 10, threadCount: 1, pipedCount: 10000);
        }

        static void MultiThreadingPipedTest2()
        {
            MultiThreadingPipedTest("MultiThreadingPipedTest2", loopCount: 2, threadCount: 5, pipedCount: 10000);
        }

        static void MultiThreadingPipedTest3()
        {
            MultiThreadingPipedTest("MultiThreadingPipedTest3", loopCount: 2, threadCount: 10, pipedCount: 10000);
        }

        static void MultiThreadingPipedTest4()
        {
            MultiThreadingPipedTest("MultiThreadingPipedTest4", loopCount: 2, threadCount: 50, pipedCount: 10000);
        }

        static void MultiThreadingPipedTest(string testName, int loopCount, int threadCount, int pipedCount)
        {
            MultiThreadingPipedTestBase(testName, loopCount, threadCount,
                (db) =>
                {
                    for (var i = 0; i < 10; i++)
                        db.Strings.Set("pipedTinyText" + i, Encoding.UTF8.GetBytes("xx" + i));
                },
                (pipe) =>
                {
                    var list = new List<RedisBytes>();
                    for (var i = 0; i < pipedCount; i++)
                        list.Add(pipe.Strings.Get("pipedTinyText" + (i % 10)));

                    pipe.Execute();

                    for (var i = 0; i < pipedCount; i++)
                    {
                        RedisByteArray data1 = list[i].Value;
                        if (data1 != "xx" + (i % 10))
                            return false;
                    }

                    return true;
                }, pipedCount);
        }

        static void MultiThreadingPipedTestBase(string testName, int loopCount, int threadCount,
                                                   Action<IRedisDb> setup, Func<IRedisPipeline, bool> func, int multiplier = 1)
        {
            threadCount = Math.Max(1, threadCount);

            using (var manager = new RedisAsyncManager(new RedisConnectionSettings("127.0.0.1", RedisPort, bulkSendFactor: 10000)))
            {
                var loopIndex = 1;
                List<Thread> thList = null;
                do
                {
                    Console.WriteLine();

                    if (setup != null)
                    {
                        using (var db = manager.GetDb(11))
                            setup(db);
                    }

                    var oldThList = thList;
                    if (oldThList != null)
                    {
                        for (var i = 0; i < oldThList.Count; i++)
                        {
                            var th = oldThList[i];
                            try
                            {
                                if (th.IsAlive)
                                    th.Interrupt();
                            }
                            catch (Exception)
                            { }
                        }
                    }

                    thList = new List<Thread>(threadCount);
                    var results = new Dictionary<string, TestResult>();

                    try
                    {
                        for (var i = 0; i < threadCount; i++)
                        {
                            var th = new Thread((obj) =>
                            {
                                var tupple = (Tuple<Thread, AutoResetEvent>)obj;

                                var @this = tupple.Item1;
                                var autoReset = tupple.Item2;

                                var ticks = 0L;
                                var failCount = 0;

                                var innerSw = new Stopwatch();
                                try
                                {
                                    using (var pipe = manager.CreatePipeline(11))
                                    {
                                        for (var j = 0; j < loopCount; j++)
                                        {
                                            var num = j.ToString();

                                            innerSw.Restart();

                                            var ok = func(pipe);

                                            innerSw.Stop();

                                            ticks += innerSw.ElapsedTicks;

                                            if (!ok)
                                                failCount++;
                                        }
                                    }
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(e);
                                }
                                finally
                                {
                                    try
                                    {
                                        var elapsedMSecs = TimeSpan.FromTicks(ticks).TotalMilliseconds;
                                        var requestCount = loopCount * multiplier;

                                        var testResult = new TestResult
                                        {
                                            TestName = testName,
                                            LoopCount = requestCount,
                                            FailCount = failCount,
                                            SumOfInnerTicks = ticks,
                                            SumOfInnerMSecs = elapsedMSecs,
                                            Concurrency = Math.Ceiling(1000 * ((double)requestCount / elapsedMSecs))
                                        };

                                        lock (results)
                                        {
                                            results[@this.Name] = testResult;
                                        }
                                    }
                                    finally
                                    {
                                        autoReset.Set();
                                    }
                                }
                            });

                            th.Name = loopIndex++.ToString("D2") + "." + i.ToString("D2");
                            th.IsBackground = true;
                            thList.Add(th);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }

                    var autoResets = new AutoResetEvent[thList.Count];
                    try
                    {
                        for (var i = 0; i < thList.Count; i++)
                        {
                            var th = thList[i];

                            var autoReset = new AutoResetEvent(false);
                            autoResets[i] = autoReset;

                            th.Start(new Tuple<Thread, AutoResetEvent>(th, autoReset));
                        }
                    }
                    finally
                    {
                        WaitHandle.WaitAll(autoResets);
                    }

                    Console.Clear();

                    var sumOfResults = new TestResult { TestName = "Sum" };

                    foreach (var kv in results)
                    {
                        var result = kv.Value;

                        sumOfResults.LoopCount += result.LoopCount;
                        sumOfResults.FailCount = result.FailCount;
                        sumOfResults.SumOfInnerTicks = result.SumOfInnerTicks;
                        sumOfResults.SumOfInnerMSecs = result.SumOfInnerMSecs;

                        Console.WriteLine();
                        WriteResult(result);
                    }

                    sumOfResults.Concurrency = Math.Ceiling(1000 * ((double)sumOfResults.LoopCount / (sumOfResults.SumOfInnerTicks / TimeSpan.TicksPerMillisecond)));

                    Console.WriteLine();
                    WriteResult(sumOfResults);
                }
                while (Console.ReadKey(true).Key != ConsoleKey.Escape);
            }
        }

        #endregion Piped Tests

        #endregion Multi-Threading

        #region Monitor Tests

        static void MonitorTest1()
        {
            do
            {
                Console.Clear();

                var count = 0;
                using (var manager = new RedisAsyncManager(new RedisConnectionSettings("127.0.0.1", RedisPort, bulkSendFactor: 10000)))
                {
                    manager.MonitorChannel.Subscribe((m) =>
                    {
                        Console.WriteLine("Time: " + m.Time);
                        Console.WriteLine("Client: " + m.ClientInfo);
                        Console.WriteLine("Command: " + m.Command);

                        if (m.Data != null)
                            Console.WriteLine("Received data: " + m.Data);
                        else
                            Console.WriteLine("Received data: ?");

                        Console.WriteLine();
                        Console.WriteLine("Press any key to escape ...");

                        if (count++ > 100)
                            manager.MonitorChannel.Quit();
                    });

                    Console.WriteLine();
                    Console.WriteLine("Press any key to escape ...");

                    if (Console.ReadKey().Key == ConsoleKey.Escape)
                        return;
                }
            } while (true);
        }

        static void MonitorTest2()
        {
            do
            {
                Console.Clear();

                using (var manager = new RedisAsyncManager(new RedisConnectionSettings("127.0.0.1", RedisPort, receiveTimeout: 90000)))
                {
                    manager.MonitorChannel.Subscribe((m) =>
                    {
                        Console.WriteLine("Time: " + m.Time);
                        Console.WriteLine("Client: " + m.ClientInfo);
                        Console.WriteLine("Command: " + m.Command);

                        if (m.Data != null)
                            Console.WriteLine("Received data: " + m.Data);
                        else
                            Console.WriteLine("Received data: ?");

                        Console.WriteLine();
                        Console.WriteLine("Press any key to escape ...");
                    });

                    Console.WriteLine();
                    Console.WriteLine("Press any key to escape ...");

                    Thread.Sleep(5000);
                    Console.WriteLine();
                    Console.WriteLine("Quiting ...");

                    manager.MonitorChannel.Quit();

                    Console.WriteLine();
                    Console.WriteLine("Quited ...");

                    Thread.Sleep(5000);

                    Console.WriteLine();

                    Console.WriteLine("Starting monitoring ...");
                    manager.MonitorChannel.Monitor();

                    Console.WriteLine();
                    Console.WriteLine("Started monitoring ...");

                    if (Console.ReadKey().Key == ConsoleKey.Escape)
                        return;
                }
            } while (true);
        }

        #endregion Monitor Tests

        #region PubSub Tests

        static void PubSubTest1()
        {
            do
            {
                Console.Clear();

                var count = 0;
                using (var manager = new RedisAsyncManager(new RedisConnectionSettings("127.0.0.1", RedisPort, bulkSendFactor: 10000)))
                {
                    manager.PubSubChannel.Subscribe((m) =>
                    {
                        Console.WriteLine("Type: " + m.Type);
                        Console.WriteLine("Channel: " + m.Channel);
                        Console.WriteLine("Pattern: " + m.Pattern);

                        if (m.Data != null)
                            Console.WriteLine("Received data: " + m.Data);
                        else
                            Console.WriteLine("Received data: ?");

                        Console.WriteLine();
                        Console.WriteLine("Press any key to escape ...");

                        if (count++ > 100)
                            manager.MonitorChannel.Quit();
                    }, "*");

                    Console.WriteLine();
                    Console.WriteLine("Press any key to escape ...");

                    if (Console.ReadKey().Key == ConsoleKey.Escape)
                        return;
                }
            } while (true);
        }

        static void PubSubTest2()
        {
            do
            {
                Console.Clear();

                using (var manager = new RedisAsyncManager(new RedisConnectionSettings("127.0.0.1", RedisPort, bulkSendFactor: 10000)))
                {
                    var channel = manager.PubSubChannel;

                    channel.PSubscribe((m) =>
                    {
                        Console.WriteLine("Type: " + m.Type);
                        Console.WriteLine("Channel: " + m.Channel);
                        Console.WriteLine("Pattern: " + m.Pattern);

                        if (m.Data != null)
                            Console.WriteLine("Received data: " + m.Data);
                        else
                            Console.WriteLine("Received data: ?");

                        Console.WriteLine();
                        Console.WriteLine("Press any key to escape ...");

                    }, "*");

                    Console.WriteLine();
                    Console.WriteLine("Press any key to escape ...");

                    Thread.Sleep(5000);
                    Console.WriteLine();
                    Console.WriteLine("Quiting ...");

                    manager.PubSubChannel.Quit();

                    Console.WriteLine();
                    Console.WriteLine("Quited ...");

                    Thread.Sleep(5000);

                    Console.WriteLine();

                    Console.WriteLine("Starting subcriptions ...");
                    manager.PubSubChannel.ResubscribeAll();

                    Console.WriteLine();
                    Console.WriteLine("Started subcriptions ...");

                    Thread.Sleep(1000);

                    for (var i = 0; i < 10; i++)
                        Console.WriteLine("Ping: " + channel.Ping());

                    if (Console.ReadKey().Key == ConsoleKey.Escape)
                        return;
                }
            } while (true);
        }

        #endregion PubSub Tests

        #region Common

        private static void WriteResult(TestResult result)
        {
            Console.WriteLine();
            Console.WriteLine("Test: " + result.TestName);
            Console.WriteLine("Loop count: " + result.LoopCount);
            Console.WriteLine("Fail count: " + result.FailCount);
            Console.WriteLine("Sum of inner ticks: " + result.SumOfInnerTicks);
            Console.WriteLine("Sum of inner msecs: " + result.SumOfInnerMSecs);
            Console.WriteLine("Average concurrency (message/sec): " + result.Concurrency);
            Console.WriteLine("Press any key to continue, ESC to escape ...");
        }

        static TestResult[] PipelineTestBase(string testName, int loopCount,
             bool continuous, Action<IRedisDb> setup, Func<IRedisPipeline, bool> func, int multiplier = 1)
        {
            var results = new List<TestResult>();

            using (var manager = new RedisAsyncManager(new RedisConnectionSettings("127.0.0.1", RedisPort, bulkSendFactor: 10000)))
            {
                do
                {
                    Console.WriteLine();

                    var ticks = 0L;
                    var failCount = 0;

                    if (setup != null)
                    {
                        using (var db = manager.GetDb(11))
                            setup(db);
                    }

                    var innerSw = new Stopwatch();
                    try
                    {
                        using (var pipe = manager.CreatePipeline(11))
                        {
                            for (var j = 0; j < loopCount; j++)
                            {
                                var num = j.ToString();

                                innerSw.Restart();

                                var ok = func(pipe);

                                innerSw.Stop();

                                ticks += innerSw.ElapsedTicks;

                                if (!ok)
                                    failCount++;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }

                    var elapsedMSecs = TimeSpan.FromTicks(ticks).TotalMilliseconds;
                    loopCount *= multiplier;

                    var testResult = new TestResult
                    {
                        TestName = testName,
                        LoopCount = loopCount,
                        FailCount = failCount,
                        SumOfInnerTicks = ticks,
                        SumOfInnerMSecs = elapsedMSecs,
                        Concurrency = Math.Ceiling(1000 * ((double)loopCount / elapsedMSecs))
                    };
                    results.Add(testResult);
                }
                while (continuous && Console.ReadKey(true).Key != ConsoleKey.Escape);

                return results.ToArray();
            }
        }

        static TestResult[] TransactionTestBase(string testName, int loopCount,
             bool continuous, Func<IRedisTransaction, bool> func, int multiplier = 1)
        {
            var results = new List<TestResult>();

            using (var manager = new RedisAsyncManager(new RedisConnectionSettings("127.0.0.1", RedisPort)))
            {
                do
                {
                    Console.WriteLine();

                    var ticks = 0L;
                    var failCount = 0;

                    var innerSw = new Stopwatch();
                    try
                    {
                        using (var trans = manager.BeginTransaction(11))
                        {
                            for (var j = 0; j < loopCount; j++)
                            {
                                var num = j.ToString();

                                innerSw.Restart();

                                var ok = func(trans);

                                innerSw.Stop();

                                ticks += innerSw.ElapsedTicks;

                                if (!ok)
                                    failCount++;

                                /* Console.WriteLine("00:00" +
                                    ", " + j.ToString("D3") +
                                    ": Processed, " + innerSw.ElapsedMilliseconds.ToString("D3") + " msec, " +
                                    (ok ? "OK" : "FAILED")); */
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }

                    var elapsedMSecs = TimeSpan.FromTicks(ticks).TotalMilliseconds;
                    loopCount *= multiplier;

                    var testResult = new TestResult
                    {
                        TestName = testName,
                        LoopCount = loopCount,
                        FailCount = failCount,
                        SumOfInnerTicks = ticks,
                        SumOfInnerMSecs = elapsedMSecs,
                        Concurrency = Math.Ceiling(1000 * ((double)loopCount / elapsedMSecs))
                    };
                    results.Add(testResult);
                }
                while (continuous && Console.ReadKey(true).Key != ConsoleKey.Escape);

                return results.ToArray();
            }
        }

        static TestResult[] SimpleTestBase(string testName, int loopCount,
            bool continuous, Func<IRedisDb, bool> func)
        {
            var results = new List<TestResult>();

            using (var manager = new RedisAsyncManager(new RedisConnectionSettings("127.0.0.1", RedisPort)))
            {
                do
                {
                    Console.WriteLine();

                    var ticks = 0L;
                    var failCount = 0;

                    var innerSw = new Stopwatch();
                    try
                    {
                        using (var db = manager.GetDb(11))
                        {
                            for (var j = 0; j < loopCount; j++)
                            {
                                var num = j.ToString();

                                innerSw.Restart();

                                var ok = func(db);

                                innerSw.Stop();

                                ticks += innerSw.ElapsedTicks;

                                if (!ok)
                                    failCount++;

                                /* Console.WriteLine("00:00" +
                                    ", " + j.ToString("D3") +
                                    ": Processed, " + innerSw.ElapsedMilliseconds.ToString("D3") + " msec, " +
                                    (ok ? "OK" : "FAILED")); */
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }

                    var elapsedMSecs = TimeSpan.FromTicks(ticks).TotalMilliseconds;

                    var testResult = new TestResult
                    {
                        TestName = testName,
                        LoopCount = loopCount,
                        FailCount = failCount,
                        SumOfInnerTicks = ticks,
                        SumOfInnerMSecs = elapsedMSecs,
                        Concurrency = Math.Ceiling(1000 * ((double)loopCount / elapsedMSecs))
                    };
                    results.Add(testResult);
                }
                while (continuous && Console.ReadKey(true).Key != ConsoleKey.Escape);

                return results.ToArray();
            }
        }

        #endregion Common

        #region Unit Tests

        static void UnitTests()
        {
            var results = UnitTestSyncPing(false);
            WriteResult(results[0]);

            results = UnitTestAsyncPing(false);
            WriteResult(results[0]);

            results = UnitTestSyncSetTiny(false);
            WriteResult(results[0]);

            results = UnitTestAsyncSetTiny(false);
            WriteResult(results[0]);

            results = UnitTestSyncGetTiny(false);
            WriteResult(results[0]);

            results = UnitTestAsyncGetTiny(false);
            WriteResult(results[0]);

            results = UnitTestSyncGetTinyTransaction(false);
            WriteResult(results[0]);

            results = UnitTestSyncGetTinyPipeline(false);
            WriteResult(results[0]);

            results = UnitTestSyncGetTinyPipelineFail(false);
            WriteResult(results[0]);
        }

        static TestResult[] UnitTestSyncGetTinyPipeline(bool continuous)
        {
            return PipelineTestBase("UnitTestSyncGetTinyPipeline", 1, continuous,
                null,
                (pipe) =>
                {
                    for (var i = 0; i < 10; i++)
                        pipe.Strings.Set("pipedTinyText" + i, Encoding.UTF8.GetBytes("xx" + i));

                    var list = new List<RedisBytes>(10);
                    for (var i = 0; i < 10; i++)
                        list.Add(pipe.Strings.Get("pipedTinyText" + i));

                    pipe.Execute();

                    for (var i = 0; i < 10; i++)
                    {
                        RedisByteArray data = list[i].Value;
                        if (data != "xx" + i)
                            return false;
                    }

                    return true;
                }, 20);
        }

        static TestResult[] UnitTestSyncGetTinyPipelineFail(bool continuous)
        {
            return PipelineTestBase("UnitTestSyncGetTinyPipeline", 1, continuous,
                null,
                (pipe) =>
                {
                    for (var i = 0; i < 10; i++)
                        pipe.Strings.Set("pipedTinyText" + i, Encoding.UTF8.GetBytes("xx" + i));

                    var list = new List<RedisBytes>(10);
                    for (var i = 0; i < 10; i++)
                        list.Add(pipe.Strings.Get("pipedTinyText" + i));

                    pipe.Strings.Set("failedKey", (string)null);

                    pipe.Execute();

                    for (var i = 0; i < 10; i++)
                    {
                        RedisByteArray data = list[i].Value;
                        if (data != "xx" + i)
                            return false;
                    }

                    return true;
                }, 20);
        }

        static TestResult[] UnitTestSyncGetTinyTransaction(bool continuous)
        {
            return TransactionTestBase("UnitTestSyncGetTinyTransaction", 1, continuous,
            (trans) =>
            {
                var set1 = trans.Strings.Set("transTinyText", Encoding.UTF8.GetBytes("xx1"));
                var get1 = trans.Strings.Get("transTinyText");
                var set2 = trans.Strings.Set("transTinyText", Encoding.UTF8.GetBytes("xx2"));
                var get2 = trans.Strings.Get("transTinyText");
                var set3 = trans.Strings.Set("transTinyText", Encoding.UTF8.GetBytes("xx3"));
                var get3 = trans.Strings.Get("transTinyText");
                var set4 = trans.Strings.Set("transTinyText", Encoding.UTF8.GetBytes("xx4"));
                var get4 = trans.Strings.Get("transTinyText");
                var set5 = trans.Strings.Set("transTinyText", Encoding.UTF8.GetBytes("xx5"));
                var get5 = trans.Strings.Get("transTinyText");
                var get6 = trans.Strings.Get("transTinyText");

                trans.Commit();

                RedisByteArray data1 = get1.Value;
                RedisByteArray data2 = get2.Value;
                RedisByteArray data3 = get3.Value;
                RedisByteArray data4 = get4.Value;
                RedisByteArray data5 = get5.Value;
                RedisByteArray data6 = get5.Value;

                return data1 == "xx1" && data2 == "xx2" && data3 == "xx3" && data4 == "xx4" && data5 == "xx5" && data6 == "xx5";
            }, 11);
        }

        static TestResult[] UnitTestSyncGetTiny(bool continuous)
        {
            return SimpleTestBase("UnitTestSyncGetTiny", 1, continuous,
                (db) =>
                {
                    var get1 = db.Strings.Get("tinyText");

                    RedisByteArray data = get1.Value;
                    return data == "xxx";
                });
        }

        static TestResult[] UnitTestAsyncGetTiny(bool continuous)
        {
            return SimpleTestBase("UnitTestAsyncGetTiny", 1, continuous,
                (db) =>
                {
                    var get1 = db.Strings.Get("tinyText");

                    RedisByteArray data = get1.Value;
                    return data == "xxx";
                });
        }

        static TestResult[] UnitTestSyncSetTiny(bool continuous)
        {
            return SimpleTestBase("UnitTestSyncSetTiny", 1, continuous,
                (db) => db.Strings.Set("tinyText", Encoding.UTF8.GetBytes("xxx")));
        }

        static TestResult[] UnitTestAsyncSetTiny(bool continuous)
        {
            return SimpleTestBase("UnitTestAsyncSetTiny", 1, continuous,
                (db) => db.Strings.Set("tinyText", Encoding.UTF8.GetBytes("xxx")));
        }

        static TestResult[] UnitTestSyncPing(bool continuous)
        {
            return SimpleTestBase("UnitTestSyncPing", 1, continuous, (db) => db.Connection.Ping());
        }

        static TestResult[] UnitTestAsyncPing(bool continuous)
        {
            return SimpleTestBase("UnitTestAsyncPing", 1, continuous, (db) => db.Connection.Ping());
        }

        #endregion Unit Tests

        #region Performance Tests

        static void PerformancePipeTests()
        {
            var loopCount = 10;

            var results = UnitTestSyncGetTinyPipeline(false);
            WriteResult(results[0]);

            var syncResults = PerformanceTestSyncPipe(loopCount, false);
            foreach (var result in syncResults)
                WriteResult(result);
        }

        static void PerformanceGetTinyTests()
        {
            var loopCount = 5000;

            var results = UnitTestSyncSetTiny(false);
            WriteResult(results[0]);

            var syncResults = PerformanceTestSyncGetTiny(loopCount, false);
            foreach (var result in syncResults)
                WriteResult(result);
        }

        static void PerformancePingTests()
        {
            var loopCount = 5000;

            var syncResults = PerformanceTestSyncPing(loopCount, false);
            foreach (var result in syncResults)
                WriteResult(result);

            var asyncResults = PerformanceTestAsyncPing(loopCount, false);
            foreach (var result in asyncResults)
                WriteResult(result);

            foreach (var result in syncResults)
                WriteResult(result);
        }

        static TestResult[] PerformanceTestSyncPipe(int loopCount, bool continuous)
        {
            var pipedCount = 10000;
            return PipelineTestBase("PerformanceTestSyncPipe", loopCount, continuous,
                (db) =>
                {
                    for (var i = 0; i < 10; i++)
                        db.Strings.Set("pipedTinyText" + i, Encoding.UTF8.GetBytes("xx" + i));
                },
                (pipe) =>
                {
                    var list = new List<RedisBytes>();
                    for (var i = 0; i < pipedCount; i++)
                        list.Add(pipe.Strings.Get("pipedTinyText" + (i % 10)));

                    pipe.Execute();

                    for (var i = 0; i < pipedCount; i++)
                    {
                        RedisByteArray data1 = list[i].Value;
                        if (data1 != "xx" + (i % 10))
                            return false;
                    }

                    return true;
                }, pipedCount);
        }

        static TestResult[] PerformanceTestSyncGetTiny(int loopCount, bool continuous)
        {
            return SimpleTestBase("PerformanceTestSyncGetTiny", loopCount, continuous,
                (db) =>
                {
                    var get1 = db.Strings.Get("tinyText");

                    RedisByteArray data = get1.Value;
                    return data == "xxx";
                });
        }

        static TestResult[] PerformanceTestAsyncGetTiny(int loopCount, bool continuous)
        {
            return SimpleTestBase("PerformanceTestAsyncGetTiny", loopCount, continuous,
                (db) =>
                {
                    var get1 = db.Strings.Get("tinyText");

                    RedisByteArray data = get1.Value;
                    return data == "xxx";
                });
        }

        static TestResult[] PerformanceTestSyncPing(int loopCount, bool continuous)
        {
            return SimpleTestBase("PerformanceTestSyncPing", loopCount, continuous, (db) => db.Connection.Ping());
        }

        static TestResult[] PerformanceTestAsyncPing(int loopCount, bool continuous)
        {
            return SimpleTestBase("PerformanceTestAsyncPing", loopCount, continuous, (db) => db.Connection.Ping());
        }

        #endregion Performance Tests

        #endregion Tests
    }
}
