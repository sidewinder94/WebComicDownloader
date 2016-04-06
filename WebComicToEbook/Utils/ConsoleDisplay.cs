using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;

using WebComicToEbook.Configuration;

namespace WebComicToEbook.Utils
{
    public class ConsoleDisplay
    {
        private static readonly ConcurrentQueue<string> PrependContent = new ConcurrentQueue<string>();

        private static readonly ConcurrentQueue<string> AppendContent = new ConcurrentQueue<string>();

        private static readonly ConcurrentDictionary<WebComicEntry, string> MainMessageDisplay =
            new ConcurrentDictionary<WebComicEntry, string>();

        private static readonly Dictionary<WebComicEntry, Queue<string>> AdditionalMessageDisplay =
            new Dictionary<WebComicEntry, Queue<string>>();

        public bool Halted = false;

        private Timer _timer;

        public ConsoleDisplay()
        {
            this._timer = new Timer(1000) { AutoReset = true, Enabled = true };
            this._timer.Elapsed += this.Display;
            this._timer.Start();
        }

        public static void AppendLine(string line)
        {
            AppendContent.Enqueue(line);
        }

        private static void RemoveLines(ConcurrentQueue<string> queue, int count)
        {
            while (count > 0)
            {
                if (queue.Count == 0)
                {
                    break;
                }

                string temp;
                queue.TryDequeue(out temp);
                count--;
            }
        }

        public static void RemoveAppendedLines(int count = 1)
        {
            RemoveLines(AppendContent, count);
        }

        public static void RemovePrependedLines(int count = 0)
        {
            RemoveLines(PrependContent, count);
        }

        public static void PrependLine(string line)
        {
            PrependContent.Enqueue(line);
        }

        public static void MainMessage(WebComicEntry entry, string content)
        {
            MainMessageDisplay[entry] = content;
        }

        public static void AddAdditionalMessageDisplay(WebComicEntry entry, string content)
        {
            lock (AdditionalMessageDisplay)
            {
                if (!AdditionalMessageDisplay.ContainsKey(entry))
                {
                    AdditionalMessageDisplay[entry] = new Queue<string>();
                }

                AdditionalMessageDisplay[entry].Enqueue(content);
            }
        }

        private void Display(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            lock (AdditionalMessageDisplay)
            {
                if (this.Halted)
                {
                    return;
                }

                Console.Clear();
                Console.SetCursorPosition(0, 0);
                foreach (var line in PrependContent)
                {
                    Console.WriteLine(line);
                }

                Console.WriteLine();

                foreach (var entry in ConsoleDisplay.MainMessageDisplay.Keys.OrderBy(k => k.Title))
                {
                    string additionalText = string.Empty;

                    if (AdditionalMessageDisplay.ContainsKey(entry))
                    {
                        additionalText = $" - {AdditionalMessageDisplay[entry].Dequeue()}";
                        if (AdditionalMessageDisplay[entry].Count > 0)
                        {
                            additionalText = AdditionalMessageDisplay[entry].Aggregate(
                                additionalText, 
                                (current, content) => current + $" - {content}");
                        }
                    }

                    Console.WriteLine($"[{entry.Title}] : {MainMessageDisplay[entry]}{additionalText}");
                }

                foreach (var line in AppendContent)
                {
                    Console.WriteLine(line);
                }
            }
        }
    }
}