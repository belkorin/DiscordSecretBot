using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DiscordSecretBot
{
    internal class Program
    {
        public static Task Main(string[] args) => new Program().MainAsync();
        public async Task MainAsync()
        {
            // Create service collection and configure our services
            var services = ConfigureServices();
            // Generate a provider
            var serviceProvider = services.BuildServiceProvider();

            // Kick off our actual code
            await serviceProvider.GetService<Bot>()!.Run();
        }

        IServiceCollection ConfigureServices()
        {
            IConfiguration config = new ConfigurationBuilder()
                .AddUserSecrets<Program>()
                .AddEnvironmentVariables()
                .Build();

            var discordConfig = new DiscordSocketConfig()
            {
                AlwaysDownloadUsers = true,
                MaxWaitBetweenGuildAvailablesBeforeReady = 1000,
                GatewayIntents = GatewayIntents.GuildMembers | GatewayIntents.AllUnprivileged | GatewayIntents.DirectMessages
            };

            var servConfig = new InteractionServiceConfig()
            {

            };

            IServiceCollection services = new ServiceCollection();

            services.AddSingleton(config);
            services.AddSingleton<DiscordSocketClient>();
            services.AddSingleton(discordConfig);
            services.AddSingleton(servConfig);
            services.AddSingleton<InteractionService>();
            services.AddSingleton<Database>();
            services.AddSingleton<OldDMBasedHandling>();
            services.AddTransient<Bot>();

            return services;
        }
    }
}