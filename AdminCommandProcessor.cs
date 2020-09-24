using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static bottest.DiscordBot;

namespace bottest
{
    public class AdminCommandProcessor
    {
        public static string ProcessCommand(MessageInfo info, Database database, DiscordSocketClient client)
        {
            var commandValue = Regex.Matches(info.CommandValue, "\"([^\"]*)\"").FirstOrDefault()?.Groups[1].Value;
            ulong? roleId = null;
            ulong? userId = null;

            switch (info.Command.ToLower())
            {
                case "help":
                    return @"Commands:

Add permitted role: `!addrole ""role name""`
Remove permitted role: `!removerole ""role name""`
List permitted roles: `!roles`

Ban user `!ban ""user name#1234""`
Unban user `!unban ""user name#1234""`
List banned users: `!bans`";

                case "addrole":
                    roleId = GetRoleId(info, client, commandValue);
                    if (roleId == null)
                        return "Missing command parameter or invalid role name";

                    database.AddValidRole(info.Guild.Id, roleId.Value);
                    return "Added role";
                case "removerole":
                    roleId = GetRoleId(info, client, commandValue);
                    if (roleId == null)
                        return "Missing command parameter or invalid role name";

                    database.DeleteValidRole(info.Guild.Id, roleId.Value);
                    return "Removed role";
                case "roles":
                    var roleIds = database.GetValidRoles(info.Guild.Id).ToArray();
                    if (roleIds.Length == 0)
                        return "No roles specified";

                    StringBuilder sb = new StringBuilder();
                    var guild = client.GetGuild(info.Guild.Id);
                    foreach(var r in roleIds)
                        sb.AppendLine(guild.GetRole(r).Name);


                    return sb.ToString();

                case "ban":
                    userId = GetUserId(info, client, commandValue);
                    if (userId == null)
                        return "Missing command parameter or invalid username";

                    database.BanUser(info.Guild.Id, userId.Value);
                    return "banned user";
                case "unban":
                    userId = GetUserId(info, client, commandValue);
                    if (userId == null)
                        return "Missing command parameter or invalid username";

                    database.UnbanUser(info.Guild.Id, userId.Value);
                    return "unbanned user";
                case "bans":
                    var userIds = database.GetBannedUsers(info.Guild.Id).ToArray();

                    if (userIds.Length == 0)
                        return "No banned users";

                    StringBuilder sb2 = new StringBuilder();
                    var guild2 = client.GetGuild(info.Guild.Id);
                    foreach (var u in userIds)
                        sb2.AppendLine(guild2.GetUser(u).ToString());

                    return sb2.ToString();
                default:
                    return "unknown command";
            }
        }

        static ulong? GetRoleId(MessageInfo info, DiscordSocketClient client, string roleName)
        {
            var role = client.GetGuild(info.Guild.Id).Roles.FirstOrDefault(x => x.Name.Equals(roleName, StringComparison.InvariantCultureIgnoreCase));

            return role?.Id;
        }
        static ulong? GetUserId(MessageInfo info, DiscordSocketClient client, string userName)
        {
            var user = client.GetGuild(info.Guild.Id).Users.FirstOrDefault(x => x.ToString().Equals(userName, StringComparison.InvariantCultureIgnoreCase));

            return user?.Id;
        }
    }
}
