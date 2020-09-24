using Discord;
using Discord.WebSocket;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace bottest
{

    public class DiscordBot
    {
        DiscordSocketClient _client;
        Database _database;

        Dictionary<ulong, MessageInfo> _pendingMessages = new Dictionary<ulong, MessageInfo>();

        public async Task BotAsync()
        {
            _client = new DiscordSocketClient();
            _database = new Database();

            var token = "NzAyNTg5MzI3NDM1MTA0MjY3.XqCXdA.5cqksauDoYVpON_s2eKDhUU5oqE";

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            _client.MessageReceived += MessageReceived;

            // Block this task until the program is closed.
            await Task.Delay(-1);
        }

        private async Task MessageReceived(SocketMessage msg)
        {
            //Ignores all bots messages
            //Where message is your received SocketMessage
            if (msg.Author.IsBot) 
                return;

            if (!(msg.Channel is IDMChannel))
                return;

            var info = ParseMessage(msg);

            if (info == null)
                return;

            if (info.UserIsBanned)
                return;

            if (info.ErrorMessage != null)
            {
                await msg.Channel.SendMessageAsync(info.ErrorMessage);
                return;
            }

            if(info.IsCommand)
            {
                string message = AdminCommandProcessor.ProcessCommand(info, _database, _client);
                await msg.Channel.SendMessageAsync(message);

                return;
            }

            await _client.GetGuild(info.Guild.Id).GetTextChannel(info.Channel.Id).SendMessageAsync(info.SecretMessage);
        }

        MessageInfo ParseMessage(SocketMessage msg)
        {
            bool isCommand = msg.Content.StartsWith("!");
            bool isSendSecret = msg.Content.StartsWith("send ", StringComparison.InvariantCultureIgnoreCase);
            bool hasPending = _pendingMessages.TryGetValue(msg.Author.Id, out var storedMessage);

            if (!isSendSecret && !isCommand && !hasPending)
                return null;

            var info = new MessageInfo();
            info.Content = msg.Content;

            string embedsMessage = CheckForEmbeds(msg);

            if(embedsMessage != null)
            {
                info.ErrorMessage = embedsMessage;
                return info;
            }

            bool isError = findServer(msg.Author, storedMessage, ref info, isCommand);

            if (isError)
                return info;

            if (isCommand)
                isError = ParseCommand(info);
            else
                isError = ParseNonCommand(msg.Author, info);

            if (isError)
                return info;

            if (isCommand)
                return info;

            string contentError = CheckForLinks(info);

            info.ErrorMessage = contentError;

            return info;
        }

        bool findServer(SocketUser author, MessageInfo pending, ref MessageInfo info, bool isCommand)
        {
            if (pending != null)
            {
                if (info.Content.Equals("cancel", StringComparison.InvariantCultureIgnoreCase))
                {
                    _pendingMessages.Remove(author.Id);
                    info.ErrorMessage = "Pending command erased";
                    return true;
                }

                if (int.TryParse(info.Content, out var number))
                {
                    if (number > pending.Guilds.Length || number < 1)
                    {
                        info.ErrorMessage = "Invalid selection";
                        return true;
                    }

                    info = pending;
                    info.Guild = info.Guilds[number - 1];

                    _pendingMessages.Remove(author.Id);
                }
                else
                {
                    info.ErrorMessage = "Invalid selection";
                    return true;
                }
            }
            else if (info.Guild == null)
            {
                var guilds = author.MutualGuilds;

                if (isCommand)
                    guilds = guilds.Where(x => x.OwnerId == author.Id).ToArray();

                if (FindServerCore(author, info, guilds, isCommand))
                    return true;
            }
            
            return false;
        }

        bool ParseCommand(MessageInfo info)
        {
            var command = Regex.Matches(info.Content, "!([^\\s]*)\\s*%?").FirstOrDefault()?.Groups[1].Value;

            int commandPrefixLength = ($"!{command} ").Length;

            info.Command = command;
            info.CommandValue = info.Content.Length > commandPrefixLength ? info.Content.Substring(prefixLength) : "";
            info.IsCommand = true;

            return false;
        }

        /// <summary></summary>
        /// <returns>True on error</returns>
        const int prefixLength = 5;
        bool ParseNonCommand(SocketUser author, MessageInfo info)
        {
            if(IsUserBanned(author, info.Guild.Id))
            {
                info.UserIsBanned = true;
                return true;
            }

            if(UserIsInValidRole(author, info.Guild.Id))
            {
                info.ErrorMessage = $"sorry, the owner of {info.Guild.Name} has restricted access to this bot";
                return true;
            }

            info.SecretMessage = info.Content.Substring(prefixLength);

            var channelName = _database.GetChannel(info.Guild.Id);

            var channel = info.Guild.Channels.FirstOrDefault(x => x.Name == channelName);

            if (channel == null)
            {
                info.ErrorMessage = $"sorry, {info.Guild.Name} doesn't have a channel to post anonymous messages to.";
                return true;
            }

            info.Channel = channel;

            return false;
        }

        /// <summary></summary>
        /// <returns>True on error</returns>
        bool FindServerCore(SocketUser author, MessageInfo info, IReadOnlyCollection<SocketGuild> guilds, bool isCommand)
        {
            string memberOwnerString = isCommand ? "a member" : "an owner";
            string messageOrCommand = isCommand ? "command" : "secret message";
            //string commandInfoString = isCommand ? "!<command> srv=\"<server name>\" <command values>" : "send_server \"<server name>\" <message>";

            if (!guilds.Any())
            {
                info.ErrorMessage = $"sorry, you aren't {memberOwnerString} of any servers this bot is";
                return true;
            }

            if(guilds.Count == 1)
            {
                info.Guild = guilds.Single();
                info.IsGuildAdmin = info.Guild.OwnerId == author.Id;
                return false;
            }
            else if (guilds.Count > 1)
            {
                StringBuilder sb = new StringBuilder();

                sb.AppendLine($"You're {memberOwnerString} of multiple servers this bot also belongs to. Which server would you like to send to?");
                int i = 1;

                var sortedGuilds = guilds.OrderBy(x => x.Name).ToArray();

                foreach (var guild in sortedGuilds)
                    sb.AppendLine($"`{i++}`   {guild.Name}");

                sb.AppendLine("Reply with the number of the server you want to send to.");
                sb.AppendLine($"Reply `cancel` to discard the pending {messageOrCommand}.");

                info.ErrorMessage = sb.ToString();
                info.Guilds = sortedGuilds;

                _pendingMessages[author.Id] = info;

                return true;
            }

            throw new Exception("something went wrong while finding servers");
        }

        string CheckForEmbeds(SocketMessage msg)
        {
            if (msg.Embeds.Any())
                return "sorry, no links";

            if (msg.Attachments.Any())
                return "sorry, no attachments";

            return null;
        }

        string CheckForLinks(MessageInfo msg)
        {
            bool hasUrl = Regex.IsMatch(msg.SecretMessage, @"((http|https):\/\/[\w\-_]+(\.[\w\-_]+)+([\w\-\.,@?^=%&amp;:/~\+#]*[\w\-\@?^=%&amp;/~\+#])?)");

            if (hasUrl)
                return "sorry, no links";

            return null;
        }

        bool IsUserBanned(SocketUser author, ulong guildId)
        {
            var userId = author.Id;

            return _database.IsUserBanned(guildId, userId);
        }

        bool UserIsInValidRole(SocketUser author, ulong guildId)
        {
            var roles = _database.GetValidRoles(guildId).ToHashSet();

            var user = _client.GetGuild(guildId).GetUser(author.Id);

            bool hasRole = user.Roles.Select(x => x.Id).Any(x => roles.Contains(x));

            return hasRole;
        }

        public class MessageInfo
        {
            public SocketGuild[] Guilds { get; set; }
            public SocketGuild Guild { get; set; }
            public SocketChannel Channel { get; set; }
            public bool IsGuildAdmin { get; set; }
            public bool IsCommand { get; set; }
            public string ErrorMessage { get; set; }
            public string SecretMessage { get; set; }
            public string Command { get; set; }
            public string CommandValue { get; set; }
            public bool UserIsBanned { get; set; }

            public string Content { get; set; }
        }
    }
}
