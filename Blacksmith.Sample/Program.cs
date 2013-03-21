using System;
using System.Timers;
using Blacksmith.Core;

namespace Blacksmith.Sample
{
    class Program
    {
        private static int _count;

        static void Main(string[] args)
        {
            var client = new Client("your_project_id", "your_token");

            var pusher = new Timer { AutoReset = true, Interval = 1000, Enabled = true };
            var one = new Timer { AutoReset = true, Interval = 2500, Enabled = true };
            var bunch = new Timer { AutoReset = true, Interval = 5000, Enabled = true };

            // let's start pushing messages into the queue
            // the queue name is based on the class name
            pusher.Elapsed += (sender, eventArgs) =>
                client
                    .Queue<MyMessage>()
                    .Push(new MyMessage { Text = string.Format("Hello, World from {0}!", ++_count) });

            // I will handle try catches for you for 
            // when it comes to deserializing and executing your processing code
            // also, I will delete the message from the queue on success!
            one.Elapsed +=
                (sender, eventArgs) =>
                client.Queue<MyMessage>()
                    .Next()
                    .OnError(Console.WriteLine)
                    .Consume((m, ctx) =>
                    {
                        Console.WriteLine("consuming one: {0}", m.Target.Text);
                    });

            // Can't wait, get a bunch of messages back and consume each one
            bunch.Elapsed +=
                (sender, eventArgs) =>
                    client.Queue<MyMessage>()
                        .Get(5)
                        .ForEach(r => r.Consume((m, ctx) =>
                        {
                            Console.WriteLine("{0} : {1}", m.Id, m.Target.Text);
                        }));

            Console.ReadLine();

            // clear the queue
            pusher.Stop();
            bunch.Stop();

            client.Queue<MyMessage>().Clear();
        }
    }

    public class MyMessage
    {
        public string Text { get; set; }
    }
}
