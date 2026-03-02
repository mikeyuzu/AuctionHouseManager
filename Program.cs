using System;
using System.Threading.Tasks;

namespace AuctionHouseManager
{
    class Program
    {
        static async Task Main()
        {
            Console.WriteLine("Auction House Manager started.");
            
            int intervalMinutes = 1;

            Console.WriteLine($"Interval: {intervalMinutes} minute(s)");
            Console.WriteLine("Processing items infinitely...");

            while (true)
            {
                try
                {
                    Console.WriteLine($"[{DateTime.Now}] Processing auction items...");
                    await AuctionHouseManager.ProcessAuctionItemsAsync();
                    Console.WriteLine($"[{DateTime.Now}] Finished processing. Waiting {intervalMinutes} minute(s) for the next run.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error during processing: {ex.Message}");
                }

                await Task.Delay(TimeSpan.FromMinutes(intervalMinutes));
            }
        }
    }
}
