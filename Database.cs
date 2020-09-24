using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace bottest
{
    public class Database
    {
        SqliteConnectionStringBuilder _connStr;
        public Database()
        {
            _connStr = new SqliteConnectionStringBuilder();

            //Use DB in project directory.  If it does not exist, create it:
            _connStr.DataSource = "./SqliteDB.db";

            using (var connection = new SqliteConnection(_connStr.ConnectionString))
            {
                connection.Open();

                var createTableCmd = connection.CreateCommand();
                createTableCmd.CommandText = "CREATE TABLE IF NOT EXISTS ValidRoles(roleId UNSIGNED BIG INT, guildId UNSIGNED BIG INT)";
                createTableCmd.ExecuteNonQuery();

                createTableCmd = connection.CreateCommand();
                createTableCmd.CommandText = "CREATE TABLE IF NOT EXISTS BannedUsers(userId UNSIGNED BIG INT, guildId UNSIGNED BIG INT)";
                createTableCmd.ExecuteNonQuery();

                createTableCmd = connection.CreateCommand();
                createTableCmd.CommandText = "CREATE TABLE IF NOT EXISTS DefaultChannel(channelName TEXT, guildId UNSIGNED BIG INT)";
                createTableCmd.ExecuteNonQuery();

            }
        }

        public IEnumerable<ulong> GetValidRoles(ulong guildId)
        {
            using(var connection = new SqliteConnection(_connStr.ConnectionString))
            {
                connection.Open();

                var queryCommand = connection.CreateCommand();
                queryCommand.CommandText = $"Select roleId from ValidRoles where guildId = {guildId}";
                using (var reader = queryCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var role = reader.GetFieldValue<ulong>(0);
                        yield return role;
                    }
                }
            }
        }

        public void DeleteValidRole(ulong guildId, ulong roleId)
        {
            using (var connection = new SqliteConnection(_connStr.ConnectionString))
            {
                connection.Open();

                var deleteCmd = connection.CreateCommand();
                deleteCmd.CommandText = $"delete from ValidRoles where guildId = {guildId} and roleId = $roleId";
                deleteCmd.Parameters.AddWithValue("$roleId", roleId);
                deleteCmd.ExecuteNonQuery();
            }
        }

        public void AddValidRole(ulong guildId, ulong roleId)
        {
            using (var connection = new SqliteConnection(_connStr.ConnectionString))
            {
                connection.Open();

                var insertCmd = connection.CreateCommand();
                insertCmd.CommandText = $"insert into ValidRoles(guildId, roleId) values ({guildId}, $roleId)";
                insertCmd.Parameters.AddWithValue("$roleId", roleId);
                insertCmd.ExecuteNonQuery();
            }
        }

        public IEnumerable<ulong> GetBannedUsers(ulong guildId)
        {
            using (var connection = new SqliteConnection(_connStr.ConnectionString))
            {
                connection.Open();

                var queryCommand = connection.CreateCommand();
                queryCommand.CommandText = $"Select userId from BannedUsers where guildId = {guildId}";
                using (var reader = queryCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var userId = reader.GetFieldValue<ulong>(0);
                        yield return userId;
                    }
                }
            }
        }

        public void UnbanUser(ulong guildId, ulong userId)
        {
            using (var connection = new SqliteConnection(_connStr.ConnectionString))
            {
                connection.Open();

                var deleteCmd = connection.CreateCommand();
                deleteCmd.CommandText = $"delete from BannedUsers where guildId = {guildId} and userId = $userId";
                deleteCmd.Parameters.AddWithValue("$userId", userId);
                deleteCmd.ExecuteNonQuery();
            }
        }

        public void BanUser(ulong guildId, ulong userId)
        {
            using (var connection = new SqliteConnection(_connStr.ConnectionString))
            {
                connection.Open();

                var insertCmd = connection.CreateCommand();
                insertCmd.CommandText = $"insert into BannedUsers(guildId, userId) values ({guildId}, $userId)";
                insertCmd.Parameters.AddWithValue("$userId", userId);
                insertCmd.ExecuteNonQuery();
            }
        }
        public bool IsUserBanned(ulong guildId, ulong userId)
        {
            using (var connection = new SqliteConnection(_connStr.ConnectionString))
            {
                connection.Open();

                var queryCmd = connection.CreateCommand();
                queryCmd.CommandText = $"select EXISTS(select 1 from BannedUsers where guildId = {guildId} and userId = $userId)";
                queryCmd.Parameters.AddWithValue("$userId", userId);

                using (var reader = queryCmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        return reader.GetInt32(0) == 1;
                    }
                }
            }

            return false;
        }

        public string GetChannel(ulong guildId)
        {
            using (var connection = new SqliteConnection(_connStr.ConnectionString))
            {
                connection.Open();

                var queryCmd = connection.CreateCommand();
                queryCmd.CommandText = $"select channelName from DefaultChannel where guildId = {guildId}";

                using (var reader = queryCmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        return reader.GetString("channelName");
                    }
                }
            }

            return "secret";
        }
    }
}
