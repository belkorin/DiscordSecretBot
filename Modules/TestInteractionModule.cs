using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordSecretBot.Modules
{
    public class TestInteractionModule : InteractionModuleBase
    {
        [SlashCommand("simple_test", "a simple test")]
        public async Task SimpleTest(string input)
        {
            await RespondAsync("testing...");
        }

        [SlashCommand("secret_test", "secret secret secrets")]
        public async Task SecretTest()
        {
            var mb = new ModalBuilder()
                        .WithTitle("Send an anonymous message")
                        .WithCustomId("secret_message_test")
                        .AddTextInput(new TextInputBuilder()
                                        .WithLabel("Your message")
                                        .WithCustomId("secret_message")
                                        .WithRequired(true)
                                        .WithStyle(TextInputStyle.Paragraph));

            await Context.Interaction.RespondWithModalAsync(mb.Build());
        }

        [ModalInteraction("secret_message_test")]
        public async Task SecretMessageTest(string _, Modal modal)
        { 
            //// Get the values of components.
            //List<SocketMessageComponentData> components =
            //    modal.Data.Components.ToList();
            //string secretMessage = components
            //    .First(x => x.CustomId == "secret_message").Value;
            //// Respond to the modal.
            await RespondAsync("hi", ephemeral: true);
        }

        [TestInteractionPrecondition()]
        [SlashCommand("condition_test", "a precondition test")]
        public async Task ConditionTest()
        {
            await RespondAsync("testing...");
        }
    }

    public class TestInteractionPrecondition : PreconditionAttribute
    {
        public override async Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo commandInfo, IServiceProvider services)
        {
            return PreconditionResult.FromError("aaaaaaaaaa");
        }
    }
}
