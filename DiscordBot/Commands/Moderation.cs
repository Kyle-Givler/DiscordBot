﻿/*
MIT License

Copyright(c) 2021 Kyle Givler
https://github.com/JoyfulReaper

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordBot.Services;
using DiscordBot.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;
using DiscordBot.Models;
using System;

namespace DiscordBot.Commands
{
    [Name("Moderation")]
    public class Moderation : ModuleBase<SocketCommandContext>
    {
        private readonly DiscordSocketClient _client;
        private readonly ILogger<Moderation> _logger;
        private readonly IServerService _servers;
        private readonly IConfiguration _configuration;
        private readonly int _prefixMaxLength;

        public Moderation(DiscordSocketClient client,
            ILogger<Moderation> logger,
            IServerService servers,
            IConfiguration configuration)
        {
            _client = client;
            _logger = logger;
            _servers = servers;
            _configuration = configuration;

            var prefixConfigValue = _configuration.GetSection("PrefixMaxLength").Value;
            if (int.TryParse(prefixConfigValue, out int maxLength))
            {
                _prefixMaxLength = maxLength;
            }
            else
            {
                _prefixMaxLength = 8;
                _logger.LogError("Unable to set max prefix length, using default: {defaultValue}", _prefixMaxLength);
            }
        }

        [Command("purge")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        [RequireBotPermission(GuildPermission.ManageMessages)]
        [Summary("Purges the given number of messages from the current channel")]
        public async Task Purge([Summary("The number of message to purge")] int amount)
        {
            await Context.Channel.TriggerTypingAsync();

            var messages = await Context.Channel.GetMessagesAsync(amount + 1).FlattenAsync();
            await (Context.Channel as SocketTextChannel).DeleteMessagesAsync(messages);

            var message = await Context.Channel.SendEmbedAsync("Purge Successful", $"{messages.Count()} messages deleted successfuly!",
                "https://clipground.com/images/bye-clipart-17.jpg");

            await Task.Delay(3000);
            await message.DeleteAsync();

            await _servers.SendLogsAsync(Context.Guild, "Messages Purged", $"{Context.User.Mention} purged {messages.Count()} messages in {Context.Channel}");

            _logger.LogInformation("{user}#{discriminator} purged {number} messages in {channel} on {server}",
                Context.User.Username, Context.User.Discriminator, amount, Context.Channel.Name, Context.Guild.Name);
        }

        [Command("prefix", RunMode = RunMode.Async)]
        [RequireUserPermission(GuildPermission.Administrator)]
        [Summary("Change the prefix")]
        public async Task Prefix([Summary("The prefix to use to address the bot")]string prefix = null)
        {
            await Context.Channel.TriggerTypingAsync();
            var myPrefix = await _servers.GetGuildPrefix(Context.Guild.Id);

            if (prefix == null)
            {
                await Context.Channel.SendEmbedAsync("Prefix", $"My Prefix is {myPrefix}",
                    "https://www.thecurriculumcorner.com/wp-content/uploads/2012/10/prefixposter.jpg");

                return;
            }

            if(prefix.Length > _prefixMaxLength)
            {
                await Context.Channel.SendEmbedAsync("Invalid Prefix",$"Prefix must be less than {_prefixMaxLength} characters.",
                    "https://www.thecurriculumcorner.com/wp-content/uploads/2012/10/prefixposter.jpg");

                return;
            }

            await _servers.ModifyGuildPrefix(Context.Guild.Id, prefix);
            await Context.Channel.SendEmbedAsync("Prefix Modified", $"The prefix has been modified to `{prefix}`.",
                     "https://www.thecurriculumcorner.com/wp-content/uploads/2012/10/prefixposter.jpg");

            await _servers.SendLogsAsync(Context.Guild, "Prefix adjusted", $"{Context.User.Mention} modifed the prefix to {prefix}");

            _logger.LogInformation("{user}#{discriminator} changed the prefix for {server} to '{prefix}'",
                Context.User.Username, Context.User.Discriminator, Context.Guild.Name, prefix);
        }

        [Command("welcome")]
        [Summary("Change user welcoming settings")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task Welcome([Summary("Option to change: channel, background or clear")]string option = null,
            [Summary("Value to assign to the option")]string value = null)
        {
            await Context.Channel.TriggerTypingAsync();

            if (option == null && value == null)
            {
                SendWelcomeChannelInformation();
                return;
            }

            if(option.ToLowerInvariant() == "channel" && value != null)
            {
                SetWelcomeChannelInformation(value);
                return;
            }

            if (option.ToLowerInvariant() == "background" && value != null)
            {
                SetWelcomeBannerBackgroundInformation(value);
                return;
            }

            if(option.ToLowerInvariant() == "clear" && value == null)
            {
                await _servers.ClearWelcomeChannel(Context.Guild.Id);
                await ReplyAsync("Successfully cleared the welcome channel!");

                await _servers.SendLogsAsync(Context.Guild, "Welcome channel cleared", $"{Context.User.Mention} cleared the welcome channel");
                _logger.LogInformation("Welcome channel cleared by {user} in server {server}", Context.User.Username, Context.Guild.Name);
                return;
            }

            await ReplyAsync("You did not use this command properly!");
        }

        [Command("mute")]
        [Summary("mute a user")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        [RequireBotPermission(GuildPermission.ManageRoles)]
        public async Task Mute([Summary("The user to mute")]SocketGuildUser user, 
            [Summary("Number of minutes to mute for")]int minutes=5, 
            [Summary("The reason for muting")][Remainder]string reason = null)
        {
            await Context.Channel.TriggerTypingAsync();

            if (user.Hierarchy > Context.Guild.CurrentUser.Hierarchy)
            {
                await Context.Channel.SendEmbedAsync("Invalid User", "That user has a higher position than the bot!",
                    "https://www.elegantthemes.com/blog/wp-content/uploads/2020/08/000-http-error-codes.png");
                return;
            }

            // Check for muted role, attempt to create it if it doesn't exist
            var role = (Context.Guild as IGuild).Roles.FirstOrDefault(x => x.Name == "Muted");
            if(role == null)
            {
                role = await Context.Guild.CreateRoleAsync("Muted", new GuildPermissions(sendMessages: false), null, false, null);
            }

            if(role.Position > Context.Guild.CurrentUser.Hierarchy)
            {
                await Context.Channel.SendEmbedAsync("Invalid permissions", "the muted role has a higher position than the bot!",
                "https://www.elegantthemes.com/blog/wp-content/uploads/2020/08/000-http-error-codes.png");
                return;
            }

            if(user.Roles.Contains(role))
            {
                await Context.Channel.SendEmbedAsync("Already Muted", "That user is already muted!",
                    "https://www.elegantthemes.com/blog/wp-content/uploads/2020/08/000-http-error-codes.png");
                return;
            }

            await role.ModifyAsync(x => x.Position = Context.Guild.CurrentUser.Hierarchy);
            foreach (var channel in Context.Guild.Channels)
            {
                if(!channel.GetPermissionOverwrite(role).HasValue || channel.GetPermissionOverwrite(role).Value.SendMessages == PermValue.Allow)
                {
                    await channel.AddPermissionOverwriteAsync(role, new OverwritePermissions(sendMessages: PermValue.Deny));
                }
            }

            MuteHandler.AddMute(new Mute { Guild = Context.Guild, User = user, End = DateTime.Now + TimeSpan.FromMinutes(minutes), Role = role });
            await user.AddRoleAsync(role);
            await Context.Channel.SendEmbedAsync($"Muted {user.Username}", $"Duration: {minutes} minutes\nReason: {reason ?? "None"}",
                "https://image.freepik.com/free-vector/no-loud-sound-mute-icon_101884-1079.jpg");

            await _servers.SendLogsAsync(Context.Guild, "Muted", $"{Context.User.Mention} muted {user.Mention}");
            _logger.LogInformation("{user} muted {target} in {server}", Context.User.Username, user.Username, Context.Guild.Name);
        }

        [Command("unmute")]
        [Summary("unmute a user")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        [RequireBotPermission(GuildPermission.ManageRoles)]
        private async Task Unmute([Summary("The user to unmute")]SocketGuildUser user)
        {
            var role = (Context.Guild as IGuild).Roles.FirstOrDefault(x => x.Name == "Muted");
            if (role == null)
            {
                await Context.Channel.SendEmbedAsync("Not Muted", "This person has not been muted!");
                return;
            }

            if (role.Position > Context.Guild.CurrentUser.Hierarchy)
            {
                await Context.Channel.SendEmbedAsync("Invalid permissions", "the muted role has a higher position than the bot!",
                "https://www.elegantthemes.com/blog/wp-content/uploads/2020/08/000-http-error-codes.png");
                return;
            }

            if (!user.Roles.Contains(role))
            {
                await Context.Channel.SendEmbedAsync("Not Muted", "This person has not been muted!",
                    "https://www.elegantthemes.com/blog/wp-content/uploads/2020/08/000-http-error-codes.png");
                return;
            }

            await user.RemoveRoleAsync(role);
            await Context.Channel.SendEmbedAsync($"Unmuted {user.Username}", "Succesfully unmuted the user",
                "https://imgaz2.staticbg.com/thumb/large/oaupload/ser1/banggood/images/21/07/9474ae00-56ad-43ba-9bf1-97c7e80d34ee.jpg.webp");

            await _servers.SendLogsAsync(Context.Guild, "Un-muted", $"{Context.User.Mention} unmuted {user.Mention}");
            _logger.LogInformation("{user} unmuted {target} in {server}", Context.User.Username, user.Username, Context.Guild.Name);
        }

        [Command("slowmode")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        [RequireBotPermission(GuildPermission.ManageChannels)]
        [Summary("Enable slowmode")]
        public async Task SlowMode(int interval = 0)
        {
            await Context.Channel.TriggerTypingAsync();
            await (Context.Channel as SocketTextChannel).ModifyAsync(x => x.SlowModeInterval = interval);
            await Context.Channel.SendEmbedAsync("Slowmode", $"The slowmode interval was adjusted to {interval} seconds!");

            await _servers.SendLogsAsync(Context.Guild, "Slow Mode", $"{Context.User.Mention} set slowmode interval to {interval} for {Context.Channel.Name}");

            _logger.LogInformation("{user} set slowmode to {value} in {server}", Context.User.Username, interval, Context.Guild.Name);
        }

        private async void SetWelcomeBannerBackgroundInformation(string value)
        {
            if (value == "clear")
            {
                await _servers.ClearBackground(Context.Guild.Id);
                await ReplyAsync("Successfully cleared background!");
                await _servers.SendLogsAsync(Context.Guild, "Background cleared", $"{Context.User} cleared the welcome image background.");
                return;
            }

            await _servers.ModifyWelcomeBackground(Context.Guild.Id, value);
            await _servers.SendLogsAsync(Context.Guild, "Background Modified", $"{Context.User} modified the welcome image background to {value}");
            await ReplyAsync($"Successfully modified the background to {value}");
        }

        private async void SetWelcomeChannelInformation(string value)
        {
            if (!MentionUtils.TryParseChannel(value, out ulong parserId))
            {
                await ReplyAsync("Please pass in a valid channel!");
                return;
            }

            var parsedChannel = Context.Guild.GetTextChannel(parserId);
            if (parsedChannel == null)
            {
                await ReplyAsync("Please pass in a valid channel!");
                return;
            }

            await _servers.ModifyWelcomeChannel(Context.Guild.Id, parserId);
            await ReplyAsync($"Successfully modified the welcome channel to {parsedChannel.Mention}");
            await _servers.SendLogsAsync(Context.Guild, "Welcome Channel Modified", $"{Context.User} modified the welcome channel to {value}");
        }

        private async void SendWelcomeChannelInformation()
        {
            var welcomeChannelId = await _servers.GetWelcomeChannel(Context.Guild.Id);
            if (welcomeChannelId == 0)
            {
                await ReplyAsync("The welcome channel has not yet been set!");
                return;
            }

            var welcomeChannel = Context.Guild.GetTextChannel(welcomeChannelId);
            if (welcomeChannel == null)
            {
                await ReplyAsync("The welcome channel has not yet been set!");
                await _servers.ClearWelcomeChannel(Context.Guild.Id);
                return;
            }

            var welcomeBackground = await _servers.GetBackground(Context.Guild.Id);
            if (welcomeBackground != null)
            {
                await ReplyAsync($"The welcome channel is {welcomeChannel.Mention}.\nThe background is {welcomeBackground}.");
            }
            else
            {
                await ReplyAsync($"The welcome channel is {welcomeChannel.Mention}.\nThe background is not set.");
            }
        }

        [Command("logs")]
        [Summary("Change logging settings")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task Logs(string option = null, string value = null)
        {
            if (option == null && value == null)
            {
                SendLoggingChannelInformation();
                return;
            }

            if (option.ToLowerInvariant() == "channel" && value != null)
            {
                SetLoggingChannelInformation(value);
                return;
            }

            if (option.ToLowerInvariant() == "clear" && value == null)
            {
                await _servers.ClearLoggingChannel(Context.Guild.Id);
                await ReplyAsync("Successfully cleared the logging channel!");
                return;
            }

            await ReplyAsync("You did not use this command properly!");
        }

        private async void SetLoggingChannelInformation(string value)
        {
            if (!MentionUtils.TryParseChannel(value, out ulong parserId))
            {
                await ReplyAsync("Please pass in a valid channel!");
                return;
            }

            var parsedChannel = Context.Guild.GetTextChannel(parserId);
            if (parsedChannel == null)
            {
                await ReplyAsync("Please pass in a valid channel!");
                return;
            }

            await _servers.ModifyLoggingChannel(Context.Guild.Id, parserId);
            await ReplyAsync($"Successfully modified the logging channel to {parsedChannel.Mention}");
        }

        private async void SendLoggingChannelInformation()
        {
            var welcomeChannelId = await _servers.GetLoggingChannel(Context.Guild.Id);
            if (welcomeChannelId == 0)
            {
                await ReplyAsync("The logging channel has not yet been set!");
                return;
            }

            var welcomeChannel = Context.Guild.GetTextChannel(welcomeChannelId);
            if (welcomeChannel == null)
            {
                await ReplyAsync("The logging channel has not yet been set!");
                await _servers.ClearLoggingChannel(Context.Guild.Id);
                return;
            }
        }
    }
}
