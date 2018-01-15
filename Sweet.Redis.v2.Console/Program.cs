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
            public double NsOp;
            public long SumOfInnerTicks;
            public double SumOfInnerMSecs;
        }

        #endregion TestResult

        private const int RedisPort = 6380; // 7000;
        private const string RedisHost = "127.0.0.1"; // "172.28.10.234"

        static void Main(string[] args)
        {
            do
            {
                Console.Clear();

                // UnitTests();

                // PerformancePingTests();
                // PerformanceGetTinyTests();
                // PerformancePipeTests();
                // PerformanceManagerGetTinyTests();

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

                // SentinelTest1();
                // SentinelTest2();
                // SentinelTest3();
                // SentinelTest4();
                // SentinelTest5();
                // SentinelTest6();
                // SentinelTest7();
                // SentinelTest8();
                // SentinelTest9();

                // ManagerTest1();
                // ManagerTest2();
                // ManagerTest3();
                // ManagerTest4();
                // ManagerTest5();
                // ManagerTest6();
                // ManagerTest7();
                // ManagerTest8();
                // ManagerTest9();
                // ManagerTest10();
                // ManagerTest11();
                // ManagerTest12();
                ManagerTest14();
            }
            while (Console.ReadKey(true).Key != ConsoleKey.Escape);
        }

        #region Tests

        #region Manager

        static void ManagerTest14()
        {
            try
            {
                var i = 0;
                var connectionString = "host=127.0.0.1:26379;masterName=mymaster;useSlaveAsMasterIfNoMasterFound=true";

                using (var manager = new RedisManager("My Manager",
                     RedisConnectionSettings.Parse<RedisManagerSettings>(connectionString)))
                {
                    Console.Clear();

                    Console.WriteLine();
                    Console.WriteLine("Started ...");

                    do
                    {
                        i = (i + 1 % 10000);

                        var ch = (char)('0' + (i % 10));
                        var text = i.ToString() + "-" + new string(ch, 3);

                        try
                        {
                            using (var db = manager.GetDb())
                            {
                                Ping(db, false);
                                SetGet(db, "tinytext", text, false);
                            }

                            Thread.Sleep(1);

                            using (var db = manager.GetDb(true))
                            {
                                Ping(db, false);
                                Get(db, "tinytext", false);
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            Console.WriteLine();
                        }
                    }
                    while (true);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        static void ManagerTest12()
        {
            try
            {
                var i = 0;
                using (var manager = new RedisManager("My Manager",
                    RedisConnectionSettings.Parse<RedisManagerSettings>("host=127.0.0.1:26379;masterName=mymaster")))
                {
                    Console.Clear();

                    Console.WriteLine();
                    Console.WriteLine("Press any key to continue, ESC to escape ...");

                    var modKey = 0;
                    do
                    {
                        var ch = (char)('0' + (i++ % 10));
                        var text = i.ToString() + "-" + new string(ch, 10);

                        try
                        {
                            using (var db = manager.GetDb())
                            {
                                Ping(db, false);
                                SetGet(db, "tinytext", text, false);
                            }

                            using (var db = manager.GetDb(true))
                            {
                                Ping(db, false);
                                Get(db, "tinytext", false);
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            Console.WriteLine();
                            Console.WriteLine("Press any key to continue, ESC to escape ...");
                            /* if (Console.ReadKey(true).Key == ConsoleKey.Escape)
                                return; */
                        }

                        modKey = (modKey + 1) % 100;
                        if (modKey == 99 && WaitForConsoleKey(50).Key == ConsoleKey.Escape)
                            return;
                    }
                    while (true);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        static void ManagerTest11()
        {
            try
            {
                var i = 0;
                using (var manager = new RedisManager("My Manager",
                    RedisConnectionSettings.Parse<RedisManagerSettings>("host=127.0.0.1:26379,127.0.0.1:26380,127.0.0.1:26381;masterName=mymaster")))
                {
                    Console.Clear();

                    Console.WriteLine();
                    Console.WriteLine("Press any key to continue, ESC to escape ...");

                    var modKey = 0;
                    do
                    {
                        var ch = (char)('0' + (i++ % 10));
                        var text = i.ToString() + "-" + new string(ch, 10);

                        try
                        {
                            using (var db = manager.GetDb())
                            {
                                Ping(db, false);
                                SetGet(db, "tinytext", text, false);
                            }

                            using (var db = manager.GetDb(true))
                            {
                                Ping(db, false);
                                Get(db, "tinytext", false);
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            Console.WriteLine();
                            Console.WriteLine("Press any key to continue, ESC to escape ...");
                            /* if (Console.ReadKey(true).Key == ConsoleKey.Escape)
                                return; */
                        }

                        modKey = (modKey + 1) % 100;
                        if (modKey == 99 && WaitForConsoleKey(50).Key == ConsoleKey.Escape)
                            return;
                    }
                    while (true);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        static void ManagerTest10()
        {
            try
            {
                var i = 0;
                using (var manager = new RedisManager("My Manager", new RedisManagerSettings(
                    new[] { new RedisEndPoint("127.0.0.1", RedisConstants.DefaultSentinelPort) },
                    masterName: "mymaster")))
                {
                    Console.Clear();

                    Console.WriteLine();
                    Console.WriteLine("Press any key to continue, ESC to escape ...");

                    var modKey = 0;
                    do
                    {
                        var ch = (char)('0' + (i++ % 10));
                        var text = i.ToString() + "-" + new string(ch, 10);

                        try
                        {
                            using (var db = manager.GetDb())
                            {
                                Ping(db, false);
                                SetGet(db, "tinytext", text, false);
                            }

                            using (var db = manager.GetDb(true))
                            {
                                Ping(db, false);
                                Get(db, "tinytext", false);
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            Console.WriteLine();
                            Console.WriteLine("Press any key to continue, ESC to escape ...");
                            if (Console.ReadKey(true).Key == ConsoleKey.Escape)
                                return;
                        }

                        modKey = (modKey + 1) % 100;
                        if (modKey == 99 && WaitForConsoleKey(50).Key == ConsoleKey.Escape)
                            return;
                    }
                    while (true);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        static ConsoleKeyInfo WaitForConsoleKey(int milliSecond)
        {
            var delay = 0;
            while (delay < milliSecond)
            {
                if (Console.KeyAvailable)
                    return Console.ReadKey(true);

                Thread.Sleep(50);
                delay += 50;
            }
            return new ConsoleKeyInfo((char)0, (ConsoleKey)0, false, false, false);
        }

        static void ManagerTest9()
        {
            var i = 0;
            var sw = new Stopwatch();

            using (var manager = new RedisManager("My Manager", new RedisManagerSettings(
                new[] { new RedisEndPoint("127.0.0.1", RedisConstants.DefaultSentinelPort) },
                masterName: "mymaster")))
            {
                do
                {
                    try
                    {
                        Console.Clear();

                        for (var j = 0; j < 100; j++)
                        {
                            var ch = (char)('0' + (i++ % 10));
                            var text = i.ToString() + "-" + new string(ch, 10);

                            sw.Restart();
                            using (var db = manager.GetDb())
                            {
                                Ping(db);
                                SetGet(db, "tinytext", text);
                            }

                            using (var db = manager.GetDb(true))
                            {
                                Ping(db);
                                Get(db, "tinytext");
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }

                    sw.Stop();

                    Console.WriteLine();
                    Console.WriteLine("Total ticks: " + sw.ElapsedTicks);
                    Console.WriteLine("Total millisecs: " + sw.ElapsedMilliseconds);

                    Console.WriteLine();
                    Console.WriteLine("Press any key to continue, ESC to escape ...");
                }
                while (Console.ReadKey(true).Key != ConsoleKey.Escape);
            }
        }

        static void ManagerTest8()
        {
            var i = 0;
            var sw = new Stopwatch();
            do
            {
                using (var manager = new RedisManager("My Manager", new RedisManagerSettings(
                    new[] { new RedisEndPoint("127.0.0.1", RedisConstants.DefaultSentinelPort),
                        new RedisEndPoint("127.0.0.1", RedisConstants.DefaultSentinelPort + 1),
                        new RedisEndPoint("127.0.0.1", RedisConstants.DefaultSentinelPort + 2)},
                    masterName: "mymaster")))
                {
                    try
                    {
                        Console.Clear();

                        for (var j = 0; j < 100; j++)
                        {
                            var ch = (char)('0' + (i++ % 10));
                            var text = i.ToString() + "-" + new string(ch, 10);

                            sw.Restart();
                            using (var db = manager.GetDb())
                            {
                                Ping(db);
                                if (j % 10 == 3)
                                    manager.Refresh();

                                SetGet(db, "tinytext", text);
                                if (j % 10 == 9)
                                    manager.Refresh();
                            }

                            using (var db = manager.GetDb(true))
                            {
                                Get(db, "tinytext");
                                if (j % 10 == 6)
                                    manager.Refresh();
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }

                sw.Stop();

                Console.WriteLine();
                Console.WriteLine("Total ticks: " + sw.ElapsedTicks);
                Console.WriteLine("Total millisecs: " + sw.ElapsedMilliseconds);

                Console.WriteLine();
                Console.WriteLine("Press any key to continue, ESC to escape ...");
            }
            while (Console.ReadKey(true).Key != ConsoleKey.Escape);
        }

        static void ManagerTest7()
        {
            var i = 0;
            var sw = new Stopwatch();
            do
            {
                using (var manager = new RedisManager("My Manager", new RedisManagerSettings(
                    new[] { RedisEndPoint.SentinelLocalHostEndPoint },
                    masterName: "mymaster")))
                {
                    try
                    {
                        Console.Clear();

                        for (var j = 0; j < 100; j++)
                        {
                            var ch = (char)('0' + (i++ % 10));
                            var text = i.ToString() + "-" + new string(ch, 10);

                            sw.Restart();

                            try
                            {
                                using (var db = manager.GetDb())
                                {
                                    Ping(db);
                                    if (j % 10 == 3)
                                        manager.Refresh();

                                    SetGet(db, "tinytext", text);
                                    if (j % 10 == 9)
                                        manager.Refresh();
                                }

                                using (var db = manager.GetDb(true))
                                {
                                    Get(db, "tinytext");
                                    if (j % 10 == 6)
                                        manager.Refresh();
                                }
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }

                sw.Stop();

                Console.WriteLine();
                Console.WriteLine("Total ticks: " + sw.ElapsedTicks);
                Console.WriteLine("Total millisecs: " + sw.ElapsedMilliseconds);

                Console.WriteLine();
                Console.WriteLine("Press any key to continue, ESC to escape ...");
            }
            while (Console.ReadKey(true).Key != ConsoleKey.Escape);
        }

        static void ManagerTest6()
        {
            var i = 0;
            var sw = new Stopwatch();
            do
            {
                using (var manager = new RedisManager("My Manager", new RedisManagerSettings(
                    new[] { RedisEndPoint.SentinelLocalHostEndPoint },
                    masterName: "mymaster")))
                {
                    try
                    {
                        Console.Clear();

                        var ch = (char)('0' + (i++ % 10));
                        var text = i.ToString() + "-" + new string(ch, 10);

                        sw.Restart();
                        using (var db = manager.GetDb())
                        {
                            Ping(db);
                            SetGet(db, "tinytext", text);
                        }

                        using (var db = manager.GetDb(true))
                        {
                            Get(db, "tinytext");
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }

                sw.Stop();

                Console.WriteLine();
                Console.WriteLine("Total ticks: " + sw.ElapsedTicks);
                Console.WriteLine("Total millisecs: " + sw.ElapsedMilliseconds);

                Console.WriteLine();
                Console.WriteLine("Press any key to continue, ESC to escape ...");
            }
            while (Console.ReadKey(true).Key != ConsoleKey.Escape);
        }

        static void ManagerTest5()
        {
            using (var manager = new RedisManager("My Manager", new RedisManagerSettings(
                new[] { RedisEndPoint.SentinelLocalHostEndPoint },
                masterName: "mymaster")))
            {
                var i = 0;
                var sw = new Stopwatch();
                do
                {
                    try
                    {
                        Console.Clear();

                        var ch = (char)('0' + (i++ % 10));
                        var text = new string(ch, 10);

                        sw.Restart();
                        using (var db = manager.GetDb(true))
                        {
                            Ping(db);
                            SetGet(db, "tinytext", text);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }

                    sw.Stop();

                    Console.WriteLine();
                    Console.WriteLine("Total ticks: " + sw.ElapsedTicks);
                    Console.WriteLine("Total millisecs: " + sw.ElapsedMilliseconds);

                    Console.WriteLine();
                    Console.WriteLine("Press any key to continue, ESC to escape ...");
                }
                while (Console.ReadKey(true).Key != ConsoleKey.Escape);
            }
        }

        static void ManagerTest4()
        {
            using (var manager = new RedisManager("My Manager", new RedisManagerSettings(
                new[] { RedisEndPoint.SentinelLocalHostEndPoint },
                masterName: "mymaster")))
            {
                var i = 0;
                var sw = new Stopwatch();
                do
                {
                    try
                    {
                        Console.Clear();

                        var ch = (char)('0' + (i++ % 10));
                        var text = new string(ch, 10);

                        sw.Restart();
                        using (var db = manager.GetDb())
                        {
                            Ping(db);
                            SetGet(db, "tinytext", text);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }

                    sw.Stop();

                    Console.WriteLine();
                    Console.WriteLine("Total ticks: " + sw.ElapsedTicks);
                    Console.WriteLine("Total millisecs: " + sw.ElapsedMilliseconds);

                    Console.WriteLine();
                    Console.WriteLine("Press any key to continue, ESC to escape ...");
                }
                while (Console.ReadKey(true).Key != ConsoleKey.Escape);
            }
        }

        static void Ping(IRedisDb db, bool writeLog = true)
        {
            var result = db.Connection.Ping();
            if (writeLog)
            {
                if (result == null)
                    Console.WriteLine("(nil)");
                else
                {
                    var value = result.Value;
                    Console.WriteLine("Response: " + value);
                    Console.WriteLine();
                }
            }
        }

        static void SetGet(IRedisDb db, string key, string value, bool writeLog = true)
        {
            var result = db.Strings.Set(key, value);
            if (result)
                Get(db, key, writeLog);
        }

        static void Get(IRedisDb db, string key, bool writeLog = true)
        {
            var result = db.Strings.Get(key);
            if (writeLog)
            {
                if (result == (string)null)
                    Console.WriteLine("(nil)");
                else
                {
                    var value = result.Value;
                    if (value == null)
                        Console.WriteLine("(nil)");
                    else
                    {
                        Console.WriteLine("Response: " + Encoding.UTF8.GetString(value));
                        Console.WriteLine();
                    }
                }
            }
        }

        static void ManagerTest3()
        {
            using (var manager = new RedisManager("My Manager", new RedisManagerSettings(
                new[] { RedisEndPoint.SentinelLocalHostEndPoint },
                masterName: "mymaster")))
            {
                var sw = new Stopwatch();
                do
                {
                    try
                    {
                        Console.Clear();

                        var runId = String.Empty;

                        sw.Restart();
                        using (var db = manager.GetDb(true))
                        {
                            Ping(db);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }

                    sw.Stop();

                    Console.WriteLine();
                    Console.WriteLine("Total ticks: " + sw.ElapsedTicks);
                    Console.WriteLine("Total millisecs: " + sw.ElapsedMilliseconds);

                    Console.WriteLine();
                    Console.WriteLine("Press any key to continue, ESC to escape ...");
                }
                while (Console.ReadKey(true).Key != ConsoleKey.Escape);
            }
        }

        static void ManagerTest2()
        {
            using (var manager = new RedisManager("My Manager", new RedisManagerSettings(
                new[] { RedisEndPoint.LocalHostEndPoint, new RedisEndPoint("localhost", 6380) },
                masterName: "mymaster")))
            {
                var sw = new Stopwatch();
                do
                {
                    try
                    {
                        Console.Clear();

                        var runId = String.Empty;

                        sw.Restart();
                        using (var db = manager.GetDb(true))
                        {
                            Ping(db);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }

                    sw.Stop();

                    Console.WriteLine();
                    Console.WriteLine("Total ticks: " + sw.ElapsedTicks);
                    Console.WriteLine("Total millisecs: " + sw.ElapsedMilliseconds);

                    Console.WriteLine();
                    Console.WriteLine("Press any key to continue, ESC to escape ...");
                }
                while (Console.ReadKey(true).Key != ConsoleKey.Escape);
            }
        }

        static void ManagerTest1()
        {
            using (var manager = new RedisManager("My Manager",
                new RedisManagerSettings(new[] { RedisEndPoint.LocalHostEndPoint }, masterName: "mymaster")))
            {
                var sw = new Stopwatch();
                do
                {
                    try
                    {
                        Console.Clear();

                        var runId = String.Empty;

                        sw.Restart();
                        using (var db = manager.GetDb(true))
                        {
                            Ping(db);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }

                    sw.Stop();

                    Console.WriteLine();
                    Console.WriteLine("Total ticks: " + sw.ElapsedTicks);
                    Console.WriteLine("Total millisecs: " + sw.ElapsedMilliseconds);

                    Console.WriteLine();
                    Console.WriteLine("Press any key to continue, ESC to escape ...");
                }
                while (Console.ReadKey(true).Key != ConsoleKey.Escape);
            }
        }

        #endregion Manager

        #region Sentinel

        static void SentinelTest9()
        {
            using (var sentinel = RedisSentinel.NewClient(new RedisSentinelSettings("127.0.0.1",
                       RedisConstants.DefaultSentinelPort, masterName: "mymaster")))
            {
                var sw = new Stopwatch();
                do
                {
                    try
                    {
                        Console.Clear();

                        sw.Restart();

                        var result = sentinel.Commands.Monitor("mymaster2", "127.0.0.1", 6379, 2);
                        if (result == null)
                            Console.WriteLine("(nil)");
                        else
                        {
                            Console.WriteLine("Monitor: " + result.Value);
                            if (result)
                            {
                                result = sentinel.Commands.Remove("mymaster2");
                                if (result == null)
                                    Console.WriteLine("(nil)");
                                else
                                    Console.WriteLine("Remove: " + result.Value);
                            }

                            Console.WriteLine();
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }

                    Console.WriteLine();
                    Console.WriteLine("Total ticks: " + sw.ElapsedTicks);
                    Console.WriteLine("Total millisecs: " + sw.ElapsedMilliseconds);

                    Console.WriteLine();
                    Console.WriteLine("Press any key to continue, ESC to escape ...");
                }
                while (Console.ReadKey(true).Key != ConsoleKey.Escape);
            }
        }

        static void SentinelTest8()
        {
            using (var sentinel = RedisSentinel.NewClient(new RedisSentinelSettings("127.0.0.1",
                       RedisConstants.DefaultSentinelPort, masterName: "mymaster")))
            {
                var sw = new Stopwatch();
                do
                {
                    try
                    {
                        Console.Clear();

                        sw.Restart();

                        var result = sentinel.Commands.Slaves("mymaster");
                        if (result == null)
                            Console.WriteLine("(nil)");
                        else
                        {
                            var slavesValue = result.Value;

                            if (slavesValue == null)
                                Console.WriteLine("(nil)");
                            else if (slavesValue.Length == 0)
                                Console.WriteLine("(empty)");
                            else
                            {
                                foreach (var slave in slavesValue)
                                {
                                    Console.WriteLine("IP address: " + slave.IPAddress);
                                    Console.WriteLine("Port: " + slave.Port);
                                    Console.WriteLine("RunId: " + slave.RunId);
                                    Console.WriteLine("Master Host: " + slave.MasterHost);
                                    Console.WriteLine("Master Link Down Time: " + slave.MasterLinkDownTime);
                                    Console.WriteLine("Master Link Status: " + slave.MasterLinkStatus);
                                    Console.WriteLine("Master Port: " + slave.MasterPort);
                                    Console.WriteLine("Slave Priority: " + slave.SlavePriority);
                                    Console.WriteLine("Slave Repl Offset: " + slave.SlaveReplOffset);
                                    Console.WriteLine("Down After Milliseconds: " + slave.DownAfterMilliseconds);
                                    Console.WriteLine("Flags: " + slave.Flags);
                                    Console.WriteLine("Last OK Ping Reply: " + slave.LastOKPingReply);
                                    Console.WriteLine("Last Ping Reply: " + slave.LastPingReply);
                                    Console.WriteLine("Last Ping Sent: " + slave.LastPingSent);
                                    Console.WriteLine("Name: " + slave.Name);
                                    Console.WriteLine();
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }

                    Console.WriteLine();
                    Console.WriteLine("Total ticks: " + sw.ElapsedTicks);
                    Console.WriteLine("Total millisecs: " + sw.ElapsedMilliseconds);

                    Console.WriteLine();
                    Console.WriteLine("Press any key to continue, ESC to escape ...");
                }
                while (Console.ReadKey(true).Key != ConsoleKey.Escape);
            }
        }

        static void SentinelTest7()
        {
            using (var sentinel = RedisSentinel.NewClient(new RedisSentinelSettings("127.0.0.1",
                       RedisConstants.DefaultSentinelPort, masterName: "mymaster")))
            {
                var sw = new Stopwatch();
                do
                {
                    try
                    {
                        Console.Clear();

                        sw.Restart();

                        var result = sentinel.Commands.Sentinels("mymaster");
                        if (result == null)
                            Console.WriteLine("(nil)");
                        else
                        {
                            var sentinelsInfo = result.Value;

                            if (sentinelsInfo == null)
                                Console.WriteLine("(nil)");
                            else if (sentinelsInfo.Length == 0)
                                Console.WriteLine("(empty)");
                            else
                            {
                                foreach (var sentinelInfo in sentinelsInfo)
                                {
                                    Console.WriteLine("IP address: " + sentinelInfo.IPAddress);
                                    Console.WriteLine("Port: " + sentinelInfo.Port);
                                    Console.WriteLine("RunId: " + sentinelInfo.RunId);
                                    Console.WriteLine("Info Refresh: " + sentinelInfo.InfoRefresh);
                                    Console.WriteLine("Last Hello Message: " + sentinelInfo.LastHelloMessage);
                                    Console.WriteLine("Pending Commands: " + sentinelInfo.PendingCommands);
                                    Console.WriteLine("Voted Leader: " + sentinelInfo.VotedLeader);
                                    Console.WriteLine("Voted Leader Epoch: " + sentinelInfo.VotedLeaderEpoch);
                                    Console.WriteLine("Down After Milliseconds: " + sentinelInfo.DownAfterMilliseconds);
                                    Console.WriteLine("Flags: " + sentinelInfo.Flags);
                                    Console.WriteLine("Last OK Ping Reply: " + sentinelInfo.LastOKPingReply);
                                    Console.WriteLine("Last Ping Reply: " + sentinelInfo.LastPingReply);
                                    Console.WriteLine("Last Ping Sent: " + sentinelInfo.LastPingSent);
                                    Console.WriteLine("Name: " + sentinelInfo.Name);
                                    Console.WriteLine();
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }

                    Console.WriteLine();
                    Console.WriteLine("Total ticks: " + sw.ElapsedTicks);
                    Console.WriteLine("Total millisecs: " + sw.ElapsedMilliseconds);

                    Console.WriteLine();
                    Console.WriteLine("Press any key to continue, ESC to escape ...");
                }
                while (Console.ReadKey(true).Key != ConsoleKey.Escape);
            }
        }

        static void SentinelTest6()
        {
            using (var sentinel = RedisSentinel.NewClient(new RedisSentinelSettings("127.0.0.1",
                       RedisConstants.DefaultSentinelPort, masterName: "mymaster")))
            {
                var sw = new Stopwatch();
                do
                {
                    try
                    {
                        Console.Clear();

                        var runId = String.Empty;

                        sw.Restart();

                        var result = sentinel.Commands.Role();
                        if (result == null)
                            Console.WriteLine("(nil)");
                        else
                        {
                            var role = result.Value;
                            if (role == null)
                                Console.WriteLine("(nil)");
                            else
                            {
                                Console.WriteLine("Name: " + role.RoleName);
                                Console.WriteLine("Role: " + role.Role);
                                Console.WriteLine();
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }

                    Console.WriteLine();
                    Console.WriteLine("Total ticks: " + sw.ElapsedTicks);
                    Console.WriteLine("Total millisecs: " + sw.ElapsedMilliseconds);

                    Console.WriteLine();
                    Console.WriteLine("Press any key to continue, ESC to escape ...");
                }
                while (Console.ReadKey(true).Key != ConsoleKey.Escape);
            }
        }

        static void SentinelTest5()
        {
            using (var sentinel = RedisSentinel.NewClient(new RedisSentinelSettings("127.0.0.1",
                       RedisConstants.DefaultSentinelPort, masterName: "mymaster")))
            {
                var sw = new Stopwatch();
                do
                {
                    try
                    {
                        Console.Clear();

                        var runId = String.Empty;

                        sw.Restart();

                        var result = sentinel.Commands.Ping();
                        if (result == null)
                            Console.WriteLine("(nil)");
                        else
                        {
                            Console.WriteLine("Ping: " + result.Value);
                            Console.WriteLine();
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }

                    Console.WriteLine();
                    Console.WriteLine("Total ticks: " + sw.ElapsedTicks);
                    Console.WriteLine("Total millisecs: " + sw.ElapsedMilliseconds);

                    Console.WriteLine();
                    Console.WriteLine("Press any key to continue, ESC to escape ...");
                }
                while (Console.ReadKey(true).Key != ConsoleKey.Escape);
            }
        }

        static void SentinelTest4()
        {
            using (var sentinel = RedisSentinel.NewClient(new RedisSentinelSettings("127.0.0.1",
                       RedisConstants.DefaultSentinelPort, masterName: "mymaster")))
            {
                var sw = new Stopwatch();
                do
                {
                    try
                    {
                        Console.Clear();

                        var runId = String.Empty;

                        var masterResult = sentinel.Commands.Master("mymaster");
                        if (masterResult != null)
                        {
                            var master = masterResult.Value;
                            if (master != null)
                                runId = master.RunId;
                        }

                        sw.Restart();

                        var result = sentinel.Commands.IsMasterDownByAddr("127.0.0.1", 6379, runId);
                        if (result == null)
                            Console.WriteLine("(nil)");
                        else
                        {
                            var isMasterDownInfo = result.Value;

                            Console.WriteLine("IsDown: " + isMasterDownInfo.IsDown);
                            Console.WriteLine("Leader: " + isMasterDownInfo.LeaderRunId);
                            Console.WriteLine("Leader Epoch: " + isMasterDownInfo.LeaderEpoch);
                            Console.WriteLine();
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }

                    Console.WriteLine();
                    Console.WriteLine("Total ticks: " + sw.ElapsedTicks);
                    Console.WriteLine("Total millisecs: " + sw.ElapsedMilliseconds);

                    Console.WriteLine();
                    Console.WriteLine("Press any key to continue, ESC to escape ...");
                }
                while (Console.ReadKey(true).Key != ConsoleKey.Escape);
            }
        }

        static void SentinelTest3()
        {
            using (var sentinel = RedisSentinel.NewClient(new RedisSentinelSettings("127.0.0.1",
                       RedisConstants.DefaultSentinelPort, masterName: "mymaster")))
            {
                var sw = new Stopwatch();
                do
                {
                    try
                    {
                        Console.Clear();

                        sw.Restart();

                        var result = sentinel.Commands.GetMasterAddrByName("mymaster");
                        if (result == null)
                            Console.WriteLine("(nil)");
                        else
                        {
                            var endpoint = result.Value;
                            if (endpoint == null)
                                Console.WriteLine("(nil)");
                            else
                            {
                                Console.WriteLine("IP address: " + endpoint.IPAddress);
                                Console.WriteLine("Port: " + endpoint.Port);
                                Console.WriteLine();
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }

                    Console.WriteLine();
                    Console.WriteLine("Total ticks: " + sw.ElapsedTicks);
                    Console.WriteLine("Total millisecs: " + sw.ElapsedMilliseconds);

                    Console.WriteLine();
                    Console.WriteLine("Press any key to continue, ESC to escape ...");
                }
                while (Console.ReadKey(true).Key != ConsoleKey.Escape);
            }
        }

        static void SentinelTest2()
        {
            using (var sentinel = RedisSentinel.NewClient(new RedisSentinelSettings("127.0.0.1",
                       RedisConstants.DefaultSentinelPort, masterName: "mymaster")))
            {
                var sw = new Stopwatch();
                do
                {
                    try
                    {
                        Console.Clear();

                        sw.Restart();

                        var result = sentinel.Commands.Masters();
                        if (result == null)
                            Console.WriteLine("(nil)");
                        else
                        {
                            var mastersValue = result.Value;

                            if (mastersValue == null)
                                Console.WriteLine("(nil)");
                            else if (mastersValue.Length == 0)
                                Console.WriteLine("(empty)");
                            else
                            {
                                foreach (var master in mastersValue)
                                {
                                    Console.WriteLine("IP address: " + master.IPAddress);
                                    Console.WriteLine("Port: " + master.Port);
                                    Console.WriteLine("RunId: " + master.RunId);
                                    Console.WriteLine("Config epoch: " + master.ConfigEpoch);
                                    Console.WriteLine("Down after milliseconds: " + master.DownAfterMilliseconds);
                                    Console.WriteLine("Failover timeout: " + master.FailoverTimeOut);
                                    Console.WriteLine("Flags: " + master.Flags);
                                    Console.WriteLine("Last OK ping reply: " + master.LastOKPingReply);
                                    Console.WriteLine("Last ping reply: " + master.LastPingReply);
                                    Console.WriteLine("Last ping sent: " + master.LastPingSent);
                                    Console.WriteLine("Name: " + master.Name);
                                    Console.WriteLine("Number of other sentinels: " + master.NumberOfOtherSentinels);
                                    Console.WriteLine("Number of slaves: " + master.NumberOfSlaves);
                                    Console.WriteLine("Parallel syncs: " + master.ParallelSyncs);
                                    Console.WriteLine("Quorum: " + master.Quorum);
                                    Console.WriteLine();
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }

                    Console.WriteLine();
                    Console.WriteLine("Total ticks: " + sw.ElapsedTicks);
                    Console.WriteLine("Total millisecs: " + sw.ElapsedMilliseconds);

                    Console.WriteLine();
                    Console.WriteLine("Press any key to continue, ESC to escape ...");
                }
                while (Console.ReadKey(true).Key != ConsoleKey.Escape);
            }
        }

        static void SentinelTest1()
        {
            using (var sentinel = RedisSentinel.NewClient(new RedisSentinelSettings("localhost",
                       RedisConstants.DefaultSentinelPort, masterName: "mymaster")))
            {
                var sw = new Stopwatch();
                do
                {
                    try
                    {
                        Console.Clear();

                        sw.Restart();

                        var result = sentinel.Commands.Master("mymaster");
                        if (result == null)
                            Console.WriteLine("(nil)");
                        else
                        {
                            var master = result.Value;
                            if (master == null)
                                Console.WriteLine("(nil)");
                            else
                            {
                                Console.WriteLine("IP address: " + master.IPAddress);
                                Console.WriteLine("Port: " + master.Port);
                                Console.WriteLine("RunId: " + master.RunId);
                                Console.WriteLine("Config epoch: " + master.ConfigEpoch);
                                Console.WriteLine("Down after milliseconds: " + master.DownAfterMilliseconds);
                                Console.WriteLine("Failover timeout: " + master.FailoverTimeOut);
                                Console.WriteLine("Flags: " + master.Flags);
                                Console.WriteLine("Last OK ping reply: " + master.LastOKPingReply);
                                Console.WriteLine("Last ping reply: " + master.LastPingReply);
                                Console.WriteLine("Last ping sent: " + master.LastPingSent);
                                Console.WriteLine("Name: " + master.Name);
                                Console.WriteLine("Number of other sentinels: " + master.NumberOfOtherSentinels);
                                Console.WriteLine("Number of slaves: " + master.NumberOfSlaves);
                                Console.WriteLine("Parallel syncs: " + master.ParallelSyncs);
                                Console.WriteLine("Quorum: " + master.Quorum);
                                Console.WriteLine();
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }

                    Console.WriteLine();
                    Console.WriteLine("Total ticks: " + sw.ElapsedTicks);
                    Console.WriteLine("Total millisecs: " + sw.ElapsedMilliseconds);

                    Console.WriteLine();
                    Console.WriteLine("Press any key to continue, ESC to escape ...");
                }
                while (Console.ReadKey(true).Key != ConsoleKey.Escape);
            }
        }

        #endregion Sentinel

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

            using (var manager = new RedisAsyncServer(new RedisConnectionSettings(RedisHost, RedisPort, bulkSendFactor: 10000, connectionCount: 2)))
            {
                var loopIndex = 1;
                do
                {
                    Console.Clear();
                    Console.WriteLine();

                    if (setup != null)
                    {
                        using (var db = manager.GetDb(11))
                            setup(db);
                    }

                    var thList = new List<Thread>(threadCount);
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
                                            Concurrency = Math.Ceiling(1000 * ((double)requestCount / elapsedMSecs)),
                                            NsOp = (elapsedMSecs * 1000000) / (double)loopCount
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

                    for (var i = 0; i < thList.Count; i++)
                    {
                        try
                        {
                            var th = thList[i];
                            if (th.IsAlive)
                                th.Abort();
                        }
                        catch (Exception)
                        { }
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
                    sumOfResults.NsOp = ((sumOfResults.SumOfInnerTicks / TimeSpan.TicksPerMillisecond) * 1000000) / (double)sumOfResults.LoopCount;

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

            using (var manager = new RedisAsyncServer(new RedisConnectionSettings(RedisHost, RedisPort, bulkSendFactor: 10000)))
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
                                            Concurrency = Math.Ceiling(1000 * ((double)requestCount / elapsedMSecs)),
                                            NsOp = (elapsedMSecs * 1000000) / (double)loopCount
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
                    sumOfResults.NsOp = ((sumOfResults.SumOfInnerTicks / TimeSpan.TicksPerMillisecond) * 1000000) / (double)sumOfResults.LoopCount;

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
                using (var manager = new RedisAsyncServer(new RedisConnectionSettings(RedisHost, RedisPort, bulkSendFactor: 10000)))
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

                using (var manager = new RedisAsyncServer(new RedisConnectionSettings(RedisHost, RedisPort, receiveTimeout: 90000)))
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
                using (var manager = new RedisAsyncServer(new RedisConnectionSettings(RedisHost, RedisPort, bulkSendFactor: 10000)))
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

                using (var manager = new RedisAsyncServer(new RedisConnectionSettings(RedisHost, RedisPort, bulkSendFactor: 10000)))
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
            Console.WriteLine("Average Nsec per Operation (ns/op): " + result.NsOp);
            Console.WriteLine("Press any key to continue, ESC to escape ...");
        }

        static TestResult[] PipelineTestBase(string testName, int loopCount,
             bool continuous, Action<IRedisDb> setup, Func<IRedisPipeline, bool> func, int multiplier = 1)
        {
            var results = new List<TestResult>();

            using (var manager = new RedisAsyncServer(new RedisConnectionSettings(RedisHost, RedisPort, bulkSendFactor: 10000)))
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
                        Concurrency = Math.Ceiling(1000 * ((double)loopCount / elapsedMSecs)),
                        NsOp = (elapsedMSecs * 1000000) / (double)loopCount
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

            using (var manager = new RedisAsyncServer(new RedisConnectionSettings(RedisHost, RedisPort)))
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
                        Concurrency = Math.Ceiling(1000 * ((double)loopCount / elapsedMSecs)),
                        NsOp = (elapsedMSecs * 1000000) / (double)loopCount
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

            using (var manager = new RedisAsyncServer(new RedisConnectionSettings(RedisHost, RedisPort)))
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
                        Concurrency = Math.Ceiling(1000 * ((double)loopCount / elapsedMSecs)),
                        NsOp = (elapsedMSecs * 1000000) / (double)loopCount
                    };
                    results.Add(testResult);
                }
                while (continuous && Console.ReadKey(true).Key != ConsoleKey.Escape);

                return results.ToArray();
            }
        }

        static TestResult[] SimpleTestManagerBase(string testName, int loopCount,
            bool readOnly, bool continuous, Func<IRedisDb, bool> func)
        {
            var results = new List<TestResult>();

            using (var manager = new RedisManager("My Manager",
                    RedisConnectionSettings.Parse<RedisManagerSettings>("host=127.0.0.1:26379;masterName=mymaster")))
            {
                do
                {
                    Console.WriteLine();

                    var ticks = 0L;
                    var failCount = 0;

                    var innerSw = new Stopwatch();
                    try
                    {
                        using (var db = manager.GetDb(readOnly, 11))
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
                        Concurrency = Math.Ceiling(1000 * ((double)loopCount / elapsedMSecs)),
                        NsOp = (elapsedMSecs * 1000000) / (double)loopCount
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

            results = UnitTestSyncError(false);
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

        static TestResult[] UnitTestSyncError(bool continuous)
        {
            return SimpleTestBase("UnitTestSyncError", 1, continuous,
                (db) =>
                {
                    var result = db.Strings.Set("key123", "");
                    return result;
                });
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

        static TestResult[] UnitTestSyncSetTinyManager(bool continuous)
        {
            return SimpleTestManagerBase("UnitTestSyncSetTinyManager", 1, false, continuous,
                (db) => db.Strings.Set("tinyText", Encoding.UTF8.GetBytes("xxx")));
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

        static void PerformanceManagerGetTinyTests()
        {
            var loopCount = 5000;

            var results = UnitTestSyncSetTinyManager(false);
            WriteResult(results[0]);

            var syncResults = PerformanceManagerTestSyncGetTiny(loopCount, false);
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

        static TestResult[] PerformanceManagerTestSyncGetTiny(int loopCount, bool continuous)
        {
            return SimpleTestManagerBase("PerformanceManagerTestSyncGetTiny", loopCount, true, continuous,
                (db) =>
                {
                    var get1 = db.Strings.Get("tinyText");

                    RedisByteArray data = get1.Value;
                    return data == "xxx";
                });
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
