using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace SRO_Clientless
{
    class Program
    {
        private static ActionBlock<DateTimeOffset> task;
        private static CancellationTokenSource wtoken;
        private static int num = 1;
        private static int maxCount = 2500;

        static void Main(string[] args)
        {
            Console.Title = "Silkroad Clientless [Accounts: 0, Users: 0]";

            Console.WriteLine("Enter the account amount to be logged in: ");

            var written = Console.ReadLine();
            num = int.Parse(written);

            PacketHandler.InitializeHandler();

            new Thread((ThreadStart)delegate
            {
                while (true)
                {
                    Console.Title = $"Silkroad Clientless [Accounts: {Data.Accounts}, Users: {Data.Users}]";
                    Thread.Sleep(1000);
                }
            }).Start();

            wtoken = new CancellationTokenSource();
            task = (ActionBlock<DateTimeOffset>)TaskHandler.CreateNeverEndingTask((now, ct) => InitializeAccounts(ct), wtoken.Token, 500);
            task.Post(DateTimeOffset.Now);

            Console.ReadLine();
        }

        public static async Task InitializeAccounts(CancellationToken ct)
        {
            if (num >= maxCount)
            {
                Console.WriteLine("Token is now cancelled");
                wtoken.Cancel();
            }

            new Client(num, ClientType.GatewayServer, "testsrv" + num.ToString(), "123", 0, "127.0.0.1", 19501).Connect();

            Console.WriteLine($"[{num}] Account has started!");

            Interlocked.Increment(ref Data.Accounts);

            num++;
        }
    }
}
