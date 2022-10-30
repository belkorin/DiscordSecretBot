using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DiscordSecretBot.Modules;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordSecretBot
{
    internal class Bot
    {
        public const ulong DeveloperID = 178613848176197632;
        DiscordSocketClient _client;
        IConfiguration _config;
        InteractionService _interactionService;
        IServiceProvider _services;
        OldDMBasedHandling _oldClient;
        Database _database;
        public Bot(IConfiguration config, IServiceProvider services, InteractionService interactionService, DiscordSocketClient client, Database database, OldDMBasedHandling oldClient)
        {
            _config = config;
            _client = client;
            _interactionService = interactionService;
            _services = services;
            _oldClient = oldClient;
            _database = database;
        }

        public async Task Run()
        {
            //  You can assign your bot token to a string, and pass that in to connect.
            //  This is, however, insecure, particularly if you plan to have your code hosted in a public repository.
            var token = _config["token"];

            // Some alternative options would be to keep your token in an Environment Variable or a standalone file.
            // var token = Environment.GetEnvironmentVariable("NameOfYourEnvironmentVariable");
            // var token = File.ReadAllText("token.txt");
            // var token = JsonConvert.DeserializeObject<AConfigurationClass>(File.ReadAllText("config.json")).Token;

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            _client.GuildAvailable += _client_GuildAvailable;

            _client.ModalSubmitted += _client_ModalSubmitted;
            _client.MessageReceived += _oldClient.MessageReceived;

            _client.InteractionCreated += async (interaction) =>
            {
                var ctx = new SocketInteractionContext(_client, interaction);
                var result = await _interactionService.ExecuteCommandAsync(ctx, _services);
            };

            await _client.SetActivityAsync(new Game("Send an anonymous message with /secret", ActivityType.CustomStatus, ActivityProperties.None));

            // Block this task until the program is closed.
            await Task.Delay(-1);
        }

        async Task _client_ModalSubmitted(SocketModal modal)
        {
            // Get the values of components.
            List<SocketMessageComponentData> components =
                modal.Data.Components.ToList();
            string message = components
                .First(x => x.CustomId == "message").Value;

            if (modal.Data.CustomId == SecretMessagesModule.ReportProblemModal)
            {
                var dev = await _client.GetUserAsync(DeveloperID);
                var channel = await dev.CreateDMChannelAsync();
                await channel.SendMessageAsync($"issue report: {message}");
                await modal.RespondAsync("message sent", ephemeral: true);
            }
            else if (modal.Data.CustomId == SecretMessagesModule.SecretMessageModal)
            {
                // Respond to the modal.

                if (modal.Channel != null)
                {
                    await modal.Channel.SendMessageAsync(message);

                    await modal.RespondAsync("message sent", ephemeral: true);
                }
            }
        }

        async Task _client_GuildAvailable(SocketGuild arg)
        {
            await _interactionService.AddModuleAsync(typeof(SecretMessagesModule), _services);
            await _interactionService.RegisterCommandsToGuildAsync(arg.Id, deleteMissing:true);
        }
    }
}
