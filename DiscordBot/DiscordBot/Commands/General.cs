﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordBot.Commands
{
    // TODO add logging
    // TODO how do we get the DI container here? Need to pull the logger out of it!
    // TODO the bot doesn't seem to know about users until they have used a command, look into that

    public class General : ModuleBase<SocketCommandContext>
    {
        private readonly ILogger<General> _logger;

        public General(ILogger<General> logger)
        {
            _logger = logger;
        }

        [Command("echo")]
        [Summary("Echoes a message")]
        // The remainder attribute parses until the end of a command
        public async Task Echo([Remainder] [Summary("The text to echo")] string message)
        {
            _logger.LogInformation("{username}#{discriminator} echoed: {message}", Context.User.Username, Context.User.Discriminator, message);
            await ReplyAsync(message);
        }

        [Command("ping")]
        [Summary ("Respongs with Pong!")]
        public async Task Ping()
        {
            _logger.LogInformation("{username}#{discriminator} invoked ping", Context.User.Username, Context.User.Discriminator);
            await ReplyAsync("Pong!");
        }

        [Command("info")]
        [Summary("Retervies some basic information about a user")]
        [Alias("user", "whois")]
        public async Task Info([Summary("Optional user to get info about")]SocketGuildUser mentionedUser = null)
        {
            if (mentionedUser == null)
            {
                mentionedUser = Context.User as SocketGuildUser;
            }

            _logger.LogInformation("{username}#{discriminator} invoked info on {target}", Context.User.Username, Context.User.Discriminator, mentionedUser);

            var builder = new EmbedBuilder()
                .WithThumbnailUrl(mentionedUser.GetAvatarUrl() ?? mentionedUser.GetDefaultAvatarUrl())
                .WithDescription("User information:")
                .WithColor(new Color(33, 176, 252))
                .AddField("User ID", mentionedUser.Id, true)
                .AddField("Discriminator", mentionedUser.Discriminator, true)
                .AddField("Created at", mentionedUser.CreatedAt.ToString("MM/dd/yyyy"), true)
                .AddField("Joined at", mentionedUser.JoinedAt.Value.ToString("MM/dd/yyyy"), true)
                .AddField("Roles", string.Join(" ", mentionedUser.Roles.Select(x => x.Mention)))
                .WithCurrentTimestamp();

            var embed = builder.Build();

            //await Context.Channel.SendMessageAsync(null, false, embed);
            await ReplyAsync(null, false, embed);
        }

        [Command("purge")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Summary("Purges the given number of messages from the current channel")]
        public async Task Purge([Summary("The number of message to purge")] int amount)
        {
            var messages = await Context.Channel.GetMessagesAsync(amount + 1).FlattenAsync();
            await (Context.Channel as SocketTextChannel).DeleteMessagesAsync(messages);

            var message = await Context.Channel.SendMessageAsync($"{messages.Count()} messages deleted successfuly!");
            await Task.Delay(2500);
            await message.DeleteAsync();

            _logger.LogInformation("{user}#{discriminator} purged {number} messages in {channel} on {server}", 
                Context.User.Username, Context.User.Discriminator, amount, Context.Channel.Name, Context.Guild.Name);
        }

        [Command("server")]
        [Summary("Retervies some basic information about a server")]
        public async Task Server()
        {
            _logger.LogInformation("{username}#{discriminator} invoked server on {target}", Context.User.Username, Context.User.Discriminator, Context.Guild.Name);

            var builder = new EmbedBuilder()
                .WithThumbnailUrl(Context.Guild.IconUrl)
                .WithDescription("Server information:")
                .WithTitle($"{Context.Guild.Name} Information")
                .WithColor(33, 176, 252)
                .AddField("Created at", Context.Guild.CreatedAt.ToString("MM/dd/yyyy"), true)
                .AddField("Member count", (Context.Guild as SocketGuild).MemberCount + " members", true)
                .AddField("Online users", (Context.Guild as SocketGuild).Users.Where(x => x.Status == UserStatus.Offline).Count() + " members", true);

            var embed = builder.Build();
            //await Context.Channel.SendMessageAsync(null, false, embed);
            await ReplyAsync(null, false, embed);
        }
    }
}
