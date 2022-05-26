using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace bottest
{
    public class Program
    {
        public static Task TheBot;

        public static void Main(string[] args)
        {
            LaunchBot();

            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });

        static void LaunchBot()
        {
            var bot = new DiscordBot();
            while (true)
            {
                TheBot = bot.BotAsync();
                TheBot.Wait();

                Thread.Sleep(5 * 60 * 1000);
            }
        }
    }
}
