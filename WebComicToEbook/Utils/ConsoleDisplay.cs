using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Timers;

using WebComicToEbook.Configuration;

namespace WebComicToEbook.Utils
{
    public class ConsoleDisplay
    {
        private static readonly ConcurrentQueue<string> PrependContent = new ConcurrentQueue<string>();
        private static readonly ConcurrentQueue<string> AppendContent = new ConcurrentQueue<string>();
        private static readonly ConcurrentDictionary<WebComicEntry, string> MessageDisplay = new ConcurrentDictionary<WebComicEntry, string>();

        public bool Halted = false;

        private Timer _timer;

        public ConsoleDisplay()
        {
            this._timer = new Timer(1000)
                              {
                                  AutoReset = true,
                                  Enabled = true
                              };
            this._timer.Elapsed += this.Display;
            this._timer.Start();
        }


        public static void AppendLine(string line)
        {
            lock (AppendContent)
            {
                ConsoleDisplay.AppendContent.Enqueue(line);
            }
        }

        public static void PrependLine(string line)
        {
            lock(PrependContent)
            {
                PrependContent.Enqueue(line);
            }
        }

        public static void AddMessageDisplay(WebComicEntry entry, string content)
        {
            lock (MessageDisplay)
            {
                if (MessageDisplay.ContainsKey(entry))
                {
                    MessageDisplay[entry] += $" - {content}";
                }
                else
                {
                    MessageDisplay[entry] = content;
                }
            }
        }

        private void Display(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            lock (MessageDisplay)
            {
                if (this.Halted) return;
                Console.Clear();
                Console.SetCursorPosition(0, 0);
                foreach (var line in PrependContent)
                {
                    Console.WriteLine(line);
                }

                foreach (var entry in ConsoleDisplay.MessageDisplay.Keys.OrderBy(k => k.Title))
                {
                    Console.WriteLine($"[{entry.Title}] : {ConsoleDisplay.MessageDisplay[entry]}");
                }
                ConsoleDisplay.MessageDisplay.Clear();

                foreach (var line in AppendContent)
                {
                    Console.WriteLine(line);
                }
            }
        }
    }
}