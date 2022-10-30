using Discord;
using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordSecretBot.Modules
{
    public class SecretMessagesModule : InteractionModuleBase<SocketInteractionContext>
    {
        public const string ReportProblemModal = "report_modal";
        public const string SecretMessageModal = "secret_message";
        Database _database;
        OldDMBasedHandling _oldDMBasedHandling;
        public SecretMessagesModule(Database database, OldDMBasedHandling oldDMBasedHandling)
        {
            _database = database;
            _oldDMBasedHandling = oldDMBasedHandling;
        }

        [SlashCommand("help", "Get help sending an anonymous message")]
        public async Task GetHelp()
        {
            await RespondAsync(
@"To send an anonymous message, use the `/secret` command in the secret messages channel. 
You will get a popup to enter your message, which will be posted to the channel by this bot.

You can also send an anonymous message without using the slash command by sending a DM to this bot, starting with `send `, followed by your message, like this:
`send I have something to discuss that I'm not entirely comfortable with`

If you think this bot isn't functioning correctly, you can either use the `/report_problem` slash command, or you can send an anonymous message to the developer by sending a DM to this bot, starting with `report ` followed by a description of your problem.", ephemeral: true);
        }

        [SlashCommand("about", "What does this bot do?")]
        public async Task About()
        {
            await RespondAsync(
@"This bot has been created to give people the ability to discuss sensitive topics without revealing their identity.
People may also use me to reply back, if they want to also be anonymous.
**Please don't abuse this service! It is not here for comedy purposes.**

This bot does not log, record, or track users and messages in any way.

Please note that interactions through this bot are still bound by Discord's Terms of Service, Community Guidelines, and Privacy Policies.
While this bot does not record any information, we have no control over what Discord may or may not store.", ephemeral: true);
        }

        [SlashCommand("report_problem", "Report a problem with this bot to the dev")]
        public async Task ReportProblem()
        {
            if (await UserIsBanned())
                await Context.Interaction.RespondAsync("Sorry, you're not allowed to use this service", ephemeral: true);

            var mb = new ModalBuilder()
                        .WithTitle("Report an issue with this bot")
                        .WithCustomId(ReportProblemModal)
                        .AddTextInput(new TextInputBuilder()
                                        .WithLabel("What's going on?")
                                        .WithCustomId("message")
                                        .WithRequired(true)
                                        .WithStyle(TextInputStyle.Paragraph));

            await Context.Interaction.RespondWithModalAsync(mb.Build());
        }

        [SlashCommand("secret", "Send an anonymous message in this channel")]
        public async Task SendSecret()
        {
            if(await UserIsBanned())
                await Context.Interaction.RespondAsync("Sorry, you're not allowed to use this service", ephemeral: true);
            if(!(await UserHasRequiredRole()))
                await Context.Interaction.RespondAsync("Sorry, you don't have the required role to use this service", ephemeral: true);
            if(!(await UserIsInValidChannel()))
                await Context.Interaction.RespondAsync("Sorry, secret messages aren't allowed in this channel", ephemeral: true);

            var mb = new ModalBuilder()
                        .WithTitle("Send an anonymous message")
                        .WithCustomId(SecretMessageModal)
                        .AddTextInput(new TextInputBuilder()
                                        .WithLabel("Your message")
                                        .WithCustomId("message")
                                        .WithRequired(true)
                                        .WithStyle(TextInputStyle.Paragraph));

            await Context.Interaction.RespondWithModalAsync(mb.Build());
        }

        async Task<bool> UserIsBanned()
        {
            var user = Context.Interaction.User;
            var guild = Context.Interaction.GuildId!.Value;

            return await Task.Run(() => _oldDMBasedHandling.IsUserBanned(user, guild));
        }
        async Task<bool> UserHasRequiredRole()
        {
            var guildId = Context.Interaction.GuildId!.Value;

            if (!_database.GetValidRoles(guildId).Any())
                return true;

            var user = Context.Interaction.User;

            return await Task.Run(() => _oldDMBasedHandling.UserIsInValidRole(user, guildId));
        }

        async Task<bool> UserIsInValidChannel()
        {
            var channel = Context.Interaction.Channel;

            var validChannel = await Task.Run(() => _database.GetChannel(Context.Interaction.GuildId!.Value));

            return channel.Name == validChannel;
        }
    }
}