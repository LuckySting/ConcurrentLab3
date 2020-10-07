using System;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;

namespace ConcurrentLab3
{
    class Program
    {
        public static string buffer;
        public static bool finish = false;
        public static bool bEmpty = true;
        public static List<string> myMessages = new List<string>();
        public static AutoResetEvent evFull = new AutoResetEvent(false);
        public static AutoResetEvent evEmpty = new AutoResetEvent(true);
        public static SemaphoreSlim semFull;
        public static SemaphoreSlim semEmpty;
        static void dumpRead(object o)
        {
            while (!finish)
            {
                if (!bEmpty)
                {
                    myMessages.Add(buffer);
                    bEmpty = true;
                }
            }
        }

        static void dumpWrite(object o)
        {
            var messages = (string[])o;
            int i = 0;
            while(i < messages.Length)
            {
                if (bEmpty)
                {
                    buffer = messages[i] + i.ToString();
                    bEmpty = false;
                    i++;
                }
            }
        }

        static void lockWrite(object o)
        {
            var messages = (string[])o;
            int i = 0;
            while(i < messages.Length)
            {
                if (bEmpty)
                {
                    lock ("write")
                    {
                        if (bEmpty)
                        {
                            buffer = messages[i] + i.ToString();
                            bEmpty = false;
                            i++;
                        }
                    }
                }
            }
        }

        static void lockRead(object o)
        {
            while (!finish)
            {
                if (!bEmpty)
                {
                    lock ("read")
                    {
                        if (!bEmpty)
                        {
                            myMessages.Add(buffer);
                            bEmpty = true;
                        }
                    }
                }
            }
        }

        static void evWrite(object o)
        {
            var messages = (string[])o;
            int i = 0;
            while (i < messages.Length)
            {
                evEmpty.WaitOne();
                buffer = messages[i] + i.ToString();
                evFull.Set();
                i++;
            }
        }

        static void evRead(object o)
        {
            while (!finish)
            {
                evFull.WaitOne();
                if (finish)
                {
                    break;
                }
                myMessages.Add(buffer);
                evEmpty.Set();
            }
        }

        static void semWrite(object o)
        {
            var messages = (string[])o;
            int i = 0;
            while (i < messages.Length)
            {
                semEmpty.Wait();
                buffer = messages[i] + i.ToString();
                semFull.Release();
                i++;
            }
        }

        static void semRead(object o)
        {
            while (!finish)
            {
                semFull.Wait();
                if (finish)
                {
                    break;
                }
                myMessages.Add(buffer);
                semEmpty.Release();
            }
        }

        static void dumpThreadsRunner(int readersNumber, int writersNumber, int writerMessagesNumber)
        {
            var writers = new Thread[writersNumber];
            var readers = new Thread[readersNumber];
            for (int i = 0; i < Math.Max(writersNumber, readersNumber); i++)
            {
                if (i < writersNumber)
                {
                    var messages = new string[writerMessagesNumber];
                    var al = ((char)(i + 65)).ToString();
                    Array.Fill<string>(messages, al);
                    writers[i] = new Thread(dumpWrite);
                    writers[i].Start(messages);
                }
                if (i < readersNumber)
                {
                    readers[i] = new Thread(dumpRead);
                    readers[i].Start();
                }
            }
            foreach (var w in writers)
            {
                w.Join();
            }
            finish = true;
            foreach (var r in readers)
            {
                r.Join();
            }
        }

        static void lockThreadsRunner(int readersNumber, int writersNumber, int writerMessagesNumber)
        {
            var writers = new Thread[writersNumber];
            var readers = new Thread[readersNumber];
            for (int i = 0; i < Math.Max(writersNumber, readersNumber); i++)
            {
                if (i < writersNumber)
                {
                    var messages = new string[writerMessagesNumber];
                    var al = ((char)(i + 65)).ToString();
                    Array.Fill<string>(messages, al);
                    writers[i] = new Thread(lockWrite);
                    writers[i].Start(messages);
                }
                if (i < readersNumber)
                {
                    readers[i] = new Thread(lockRead);
                    readers[i].Start();
                }
            }
            foreach (var w in writers)
            {
                w.Join();
            }
            finish = true;
            foreach (var r in readers)
            {
                evFull.Set();
                r.Join();
            }
        }

        static void evThreadsRunner(int readersNumber, int writersNumber, int writerMessagesNumber)
        {
            var writers = new Thread[writersNumber];
            var readers = new Thread[readersNumber];
            for (int i = 0; i < Math.Max(writersNumber, readersNumber); i++)
            {
                if (i < writersNumber)
                {
                    var messages = new string[writerMessagesNumber];
                    var al = ((char)(i + 65)).ToString();
                    Array.Fill<string>(messages, al);
                    writers[i] = new Thread(evWrite);
                    writers[i].Start(messages);
                }
                if (i < readersNumber)
                {
                    readers[i] = new Thread(evRead);
                    readers[i].Start();
                }
            }
            foreach (var w in writers)
            {
                w.Join();
            }
            finish = true;
            foreach (var r in readers)
            {
                evFull.Set();
                r.Join();
            }
        }

        static void semThreadsRunner(int readersNumber, int writersNumber, int writerMessagesNumber)
        {
            var writers = new Thread[writersNumber];
            var readers = new Thread[readersNumber];
            for (int i = 0; i < Math.Max(writersNumber, readersNumber); i++)
            {
                if (i < writersNumber)
                {
                    var messages = new string[writerMessagesNumber];
                    var al = ((char)(i + 65)).ToString();
                    Array.Fill<string>(messages, al);
                    writers[i] = new Thread(semWrite);
                    writers[i].Start(messages);
                }
                if (i < readersNumber)
                {
                    readers[i] = new Thread(semRead);
                    readers[i].Start();
                }
            }
            foreach (var w in writers)
            {
                w.Join();
            }
            finish = true;
            semFull.Release(readersNumber);
            foreach (var r in readers)
            {
                r.Join();
            }
        }
        static void Main(string[] args)
        {
            Console.BackgroundColor = ConsoleColor.White;
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Black;
            int totalMes = 100000;
            int writers = 20;
            int readers = 20;
            var sw = new Stopwatch();
            semFull = new SemaphoreSlim(0, 20);
            semEmpty = new SemaphoreSlim(1, 20);
            sw.Start();
            // dumpThreadsRunner(readers, writers, totalMes);
            // lockThreadsRunner(readers, writers, totalMes);
             evThreadsRunner(readers, writers, totalMes);
            // semThreadsRunner(readers, writers, totalMes);
            sw.Stop();
            Console.WriteLine("Elapsed time {0} ms", sw.Elapsed.TotalMilliseconds);
            Console.WriteLine("Writers {0}; Readers {1}", writers, readers);
            Console.WriteLine("Total messages writed {0}", totalMes * writers);
            Console.WriteLine("Total messages readed {0}", myMessages.Count);
            Console.WriteLine("First {0} messages", Math.Min(20, myMessages.Count));
            for (int i = 0; i < Math.Min(20, myMessages.Count); i++)
            {
                Console.WriteLine(myMessages[i]);
            }
        }
    }
}
