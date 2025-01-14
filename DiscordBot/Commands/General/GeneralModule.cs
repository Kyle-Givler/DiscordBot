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
using DiscordBotLib.DataAccess;
using DiscordBotLib.Helpers;
using DiscordBotLib.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordBot.Commands
{
    // So it turns out that the bot needs the Presence and Server member intent in order for
    // All of the members of a channel to be "in scope"
    [Name("General")]
    public class GeneralModule : ModuleBase<SocketCommandContext>
    {
        private readonly ILogger<GeneralModule> _logger;
        private readonly DiscordSocketClient _client;
        private readonly BannerImageService _bannerImageService;
        private readonly IServerService _servers;
        private readonly IUserTimeZonesRepository _userTimeZones;
        private readonly ISettings _settings;

        public GeneralModule(ILogger<GeneralModule> logger,
            DiscordSocketClient client,
            BannerImageService bannerImageService,
            IServerService servers,
            IUserTimeZonesRepository userTimeZones,
            ISettings settings)
        {
            _logger = logger;
            _client = client;
            _bannerImageService = bannerImageService;
            _servers = servers;
            _userTimeZones = userTimeZones;
            _settings = settings;
        }

        [Command("invite")]
        [Summary("invite the bot to your server!")]
        public async Task Invite()
        {
            await Context.Channel.TriggerTypingAsync();

            _logger.LogInformation("{username}#{discriminator} executed uptime: on {server}/{channel}",
                Context.User.Username, Context.User.Discriminator, Context.Guild?.Name ?? "DM", Context.Channel.Name);

            await Context.Channel.SendEmbedAsync("Invite Link", $"Follow the link to invite {_settings.BotName}!\n{_settings.InviteLink}",
                ColorHelper.GetColor(await _servers.GetServer(Context.Guild)), ImageLookupUtility.GetImageUrl("INVITE_IMAGES"));

            //await ReplyAsync(_settings.InviteLink);
        }

        [Command("uptime")]
        [Alias("proc", "memory")]
        [Summary("Get bot uptime and memory usage")]
        public async Task ProcInfo()
        {
            await Context.Channel.TriggerTypingAsync();

            _logger.LogInformation("{username}#{discriminator} executed uptime: on {server}/{channel}",
                Context.User.Username, Context.User.Discriminator, Context.Guild?.Name ?? "DM", Context.Channel.Name);

            var process = Process.GetCurrentProcess();
            var memoryMb = Math.Round((double)process.PrivateMemorySize64 / (1e+6), 2);
            var startTime = process.StartTime;

            var upTime = DateTime.Now - startTime;

            await ReplyAsync($"Uptime: `{upTime}`\nMemory usage: `{memoryMb} MB`");
            /*
             * Not producing correct results :(
            if(LavaLinkHelper.isLavaLinkRunning())
            {
                var lavaMb = Math.Round((double)LavaLinkHelper.LavaLink.PrivateMemorySize64 / (1e+6), 2);
                await ReplyAsync($"Lavalink Memory: {lavaMb} MB");
            }
            */
            process.Dispose();
        }

        [Command("servers")]
        [Summary("Report the number of servers the bot it in")]
        public async Task Servers()
        {
            await Context.Channel.TriggerTypingAsync();

            _logger.LogInformation("{username}#{discriminator} executed servers: on {server}/{channel}",
                Context.User.Username, Context.User.Discriminator, Context.Guild?.Name ?? "DM", Context.Channel.Name);

            await ReplyAsync($"I am in {Context.Client.Guilds.Count} servers!");
        }

        [Command("math")]
        [Alias("calculate", "calculator", "evaluate", "eval", "calc")]
        [Summary("Do math!")]
        public async Task DoMath([Summary("Equation to solve")][Remainder] string math)
        {
            await Context.Channel.TriggerTypingAsync();

            _logger.LogInformation("{username}#{discriminator} executed math: {math} on {server}/{channel}",
                Context.User.Username, Context.User.Discriminator, math, Context.Guild?.Name ?? "DM", Context.Channel.Name);

            var dt = new DataTable();

            var message = await ReplyAsync(ImageLookupUtility.GetImageUrl("MATH_IMAGES"));
            try
            {
                var result = dt.Compute(math, null);
                await ReplyAsync($"Result: `{result}`");
            }
            catch (EvaluateException)
            {
                await ReplyAsync("Unable to evaluate");
            }
            catch (SyntaxErrorException)
            {
                await ReplyAsync("Syntax error");
            }
            await Task.Delay(2500);
            await message.DeleteAsync();
        }

        [Command ("about")]
        [Summary("Information about the bot itself")]
        public async Task About()
        {
            await Context.Channel.TriggerTypingAsync();

            _logger.LogInformation("{username}#{discriminator} executed about on {server}/{channel}",
                Context.User.Username, Context.User.Discriminator, Context.Guild?.Name ?? "DM", Context.Channel.Name);

            var server = await _servers.GetServer(Context.Guild);
            var prefix = server?.Prefix;
            if(prefix == null)
            {
                prefix = string.Empty;
            }

            var builder = new EmbedBuilder()
                .WithThumbnailUrl(_client.CurrentUser.GetAvatarUrl() ?? _client.CurrentUser.GetDefaultAvatarUrl())
                .WithDescription($"{_settings.BotName}\nMIT License Copyright(c) 2021 JoyfulReaper\n{_settings.BotWebsite}\n\n" +
                $"See `{prefix}invite` for the link to invite DiscordBot to your server!")
                .WithColor(ColorHelper.GetColor(server))
                .WithCurrentTimestamp();

            var embed = builder.Build();
            await ReplyAsync(null, false, embed);
        }

        [Command("owner")]
        [Summary("Retreive the server owner")]
        public async Task Owner()
        {
            await Context.Channel.TriggerTypingAsync();

            _logger.LogInformation("{username}#{discriminator} executed owner on {server}/{channel}",
                Context.User.Username, Context.User.Discriminator, Context.Guild?.Name ?? "DM", Context.Channel.Name);

            var server = await _servers.GetServer(Context.Guild);
            if(Context.Guild == null)
            {
                await Context.Channel.SendEmbedAsync($"{_settings.BotName}", $"DiscordBot was written by JoyfulReaper\n{_settings.BotWebsite}", 
                    ColorHelper.RandomColor(), _client.CurrentUser.GetAvatarUrl() ?? _client.CurrentUser.GetDefaultAvatarUrl());
                return;
            }

            var builder = new EmbedBuilder()
                .WithThumbnailUrl(Context?.Guild?.Owner.GetAvatarUrl() ?? _client.CurrentUser.GetDefaultAvatarUrl())
                .WithDescription($"{Context?.Guild?.Owner.Username} is the owner of {Context.Guild.Name}")
                .WithColor(ColorHelper.GetColor(server))
                .WithCurrentTimestamp();

            var embed = builder.Build();
            await ReplyAsync(null, false, embed);
        }

        [Command("echo")]
        [Alias("say")]
        [Summary("Echoes a message")]
        // The remainder attribute parses until the end of a command
        public async Task Echo([Remainder] [Summary("The text to echo")] string message)
        {
            await Context.Channel.TriggerTypingAsync();

            _logger.LogInformation("{username}#{discriminator} executed echo {message} on {server}/{channel}",
                Context.User.Username, Context.User.Discriminator, message, Context.Guild?.Name ?? "DM", Context.Channel.Name);

            var server = await _servers.GetServer(Context.Guild);
            var checkString = message.Replace(".", String.Empty).Replace('!', 'i').Replace("-", String.Empty).Replace("*", String.Empty);
            var filter = ProfanityHelper.GetProfanityFilterForServer(server);
            var badWords = await ProfanityHelper.GetProfanity(server, checkString);

            if (badWords.Count > 0)
            {
                await ReplyAsync("I'm not going to say that!");
                return;
            }

            await ReplyAsync($"`{message}`");
        }

        [Command("ping")]
        [Alias("latency")]
        [Summary ("Latency to server!")]
        public async Task Ping()
        {
            await Context.Channel.TriggerTypingAsync();

            _logger.LogInformation("{username}#{discriminator} executed ping on {server}/{channel}",
                Context.User.Username, Context.User.Discriminator, Context.Guild?.Name ?? "DM", Context.Channel.Name);

            var server = await _servers.GetServer(Context.Guild);

            var builder = new EmbedBuilder();
            builder
                .WithThumbnailUrl(_client.CurrentUser.GetAvatarUrl() ?? _client.CurrentUser.GetDefaultAvatarUrl())
                .WithTitle("Ping Results")
                .WithDescription("Pong!")
                .AddField("Round-trip latency to the WebSocket server (ms):", _client.Latency, false)
                .WithColor(ColorHelper.GetColor(server))
                .WithCurrentTimestamp();

            await ReplyAsync(null, false, builder.Build());
        }

        [Command("info")]
        [Summary("Retervies some basic information about a user")]
        [Alias("user", "whois")]
        public async Task Info([Summary("Optional user to get info about")]SocketUser mentionedUser = null)
        {
            await Context.Channel.TriggerTypingAsync();

            if (mentionedUser == null)
            {
                mentionedUser = Context.User; //as SocketGuildUser;
            }

            _logger.LogInformation("{username}#{discriminator} executed info ({target}) on {server}/{channel}",
                Context.User.Username, Context.User.Discriminator, mentionedUser?.Username ?? "self", Context.Guild?.Name ?? "DM", Context.Channel.Name);

            var server = await _servers.GetServer(Context.Guild);

            var builder = new EmbedBuilder()
                .WithThumbnailUrl(mentionedUser.GetAvatarUrl() ?? mentionedUser.GetDefaultAvatarUrl())
                .WithDescription("User information:")
                .WithColor(ColorHelper.GetColor(server))
                .AddField("User ID", mentionedUser.Id, true)
                .AddField("Discriminator", mentionedUser.Discriminator, true)
                .AddField("Created at", mentionedUser.CreatedAt.ToString("MM/dd/yyyy"), true)
                .WithCurrentTimestamp();

            var timezone = await _userTimeZones.GetByUserID(mentionedUser.Id);
            if(timezone != null)
            {
                builder.AddField("Timezone", timezone.TimeZone, true);
            }

            SocketGuildUser guildUser = mentionedUser as SocketGuildUser;
            if (guildUser != null)
            {
                builder
                    .AddField("Joined at", guildUser.JoinedAt.Value.ToString("MM/dd/yyyy"), true)
                    .AddField("Roles", string.Join(" ", guildUser.Roles.Select(x => x.Mention)));
            }

            var embed = builder.Build();
            await ReplyAsync(null, false, embed);
        }

        [Command("server")]
        [Summary("Retervies some basic information about a server")]
        public async Task Server()
        {
            await Context.Channel.TriggerTypingAsync();

            _logger.LogInformation("{username}#{discriminator} executed server on {server}/{channel}",
                Context.User.Username, Context.User.Discriminator, Context.Guild?.Name ?? "DM", Context.Channel.Name);

            if(await ServerHelper.CheckIfContextIsDM(Context))
            {
                return;
            }

            var builder = new EmbedBuilder()
                .WithThumbnailUrl(Context.Guild.IconUrl)
                .WithDescription("Server information:")
                .WithTitle($"{Context.Guild.Name} Information")
                .WithColor(ColorHelper.GetColor(await _servers.GetServer(Context.Guild)))
                .AddField("Created at", Context.Guild.CreatedAt.ToString("MM/dd/yyyy"), true)
                .AddField("Member count", (Context.Guild as SocketGuild).MemberCount + " members", true)
                .AddField("Online users", (Context.Guild as SocketGuild).Users.Where(x => x.Status == UserStatus.Offline).Count() + " members", true)
                .WithCurrentTimestamp();

            var embed = builder.Build(); 
            await ReplyAsync(null, false, embed);
        }

        [Command("image", RunMode = RunMode.Async)]
        [Alias("banner")]
        [Summary("Show the image banner thing")]
        public async Task Image([Summary("The user to show a banner for")] SocketGuildUser user = null)
        {
            await Context.Channel.TriggerTypingAsync();

            _logger.LogInformation("{username}#{discriminator} executed image on {server}/{channel}",
                Context.User.Username, Context.User.Discriminator, Context.Guild?.Name ?? "DM", Context.Channel.Name);

            if (await ServerHelper.CheckIfContextIsDM(Context))
            {
                return;
            }

            if (user == null)
            {
                user = Context.Message.Author as SocketGuildUser;
            }

            var background = await _servers.GetBackground(user.Guild.Id);

            var memoryStream = await _bannerImageService.CreateImage(user, background);
            memoryStream.Seek(0, SeekOrigin.Begin);
            await Context.Channel.SendFileAsync(memoryStream, $"{user.Username}.png");
        }
    }
}
