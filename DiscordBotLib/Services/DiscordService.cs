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
using DiscordBotLib.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Reflection;
using System.Threading.Tasks;
using Victoria;

namespace DiscordBotLib.Services
{
    public class DiscordService : IChatService
    {
        public static bool ShowJoinAndPartMessages { get; set; }

        private readonly IServiceProvider _serviceProvider;
        private readonly DiscordSocketClient _client;
        private readonly IConfiguration _configuration;
        private readonly CommandService _commands;
        private readonly ILogger _logger;
        private readonly IDiscordBotSettingsRepository _discordBotSettingsRepository;
        private readonly LavaNode _lavaNode;
        private readonly IServerService _servers;
        private readonly ISettings _settings;

        public DiscordService(IServiceProvider serviceProvider,
            DiscordSocketClient client,
            IConfiguration configuration,
            CommandService commands,
            ILogger<DiscordService> logger,
            IDiscordBotSettingsRepository discordBotSettingsRepository,
            LavaNode lavaNode,
            IServerService servers,
            ISettings settings)
        {
            _serviceProvider = serviceProvider;
            _client = client;
            _configuration = configuration;
            _commands = commands;
            _logger = logger;
            _discordBotSettingsRepository = discordBotSettingsRepository;
            _lavaNode = lavaNode;
            _servers = servers;
            _settings = settings;

            _client.Ready += OnReady;
            _client.MessageReceived += OnMessageReceived;
            _client.Disconnected += OnDisconncted;
        }

        private Task OnMessageReceived(SocketMessage arg)
        {
            Console.WriteLine($"Message received: {arg.Author.Username} : {arg.Channel.Name} : {arg.Content}");
            _logger.LogInformation("Message Received: {author} : {channel} : {message}", arg.Author.Username, arg.Channel.Name, arg.Content);

            return Task.CompletedTask;
        }

        private Task OnDisconncted(Exception arg)
        {
            _logger.LogError(arg, "SocketClient disconnected!");
            Console.WriteLine("SocketClient disconnected!");

            return Task.CompletedTask;
        }

        private async Task OnReady()
        {
            _logger.LogInformation("Connected as {username}#{discriminator}", _client.CurrentUser.Username, _client.CurrentUser.Discriminator);

            if (_settings.EnableLavaLink && !_lavaNode.IsConnected)
            {
                _logger.LogDebug("Connecting to Lavalink");
                await _lavaNode.ConnectAsync();
            }

            var settings = await _discordBotSettingsRepository.Get();
            await _client.SetGameAsync(settings.Game);

            Console.WriteLine("SocketClient is ready");
            Console.WriteLine($"Connected as {_client.CurrentUser.Username}#{_client.CurrentUser.Discriminator}");

            if (ShowJoinAndPartMessages)
            {
                foreach (var guild in _client.Guilds)
                {
                    foreach (var channel in guild.Channels)
                    {
                        if (channel.Name.ToLowerInvariant() == "bot" || channel.Name.ToLowerInvariant().Contains("bot-spam"))
                        {
                            if (channel != null && channel is SocketTextChannel textChannel)
                            {
                                var builder = new EmbedBuilder()
                                    .WithThumbnailUrl(_client.CurrentUser.GetAvatarUrl() ?? _client.CurrentUser.GetDefaultAvatarUrl())
                                    .WithDescription($"{_settings.BotName} Starting\nMIT License Copyright(c) 2021 JoyfulReaper\n{_settings.BotWebsite}")
                                    .WithColor(await _servers.GetEmbedColor(guild.Id))
                                    .WithCurrentTimestamp();

                                var embed = builder.Build();
                                await textChannel.SendMessageAsync(null, false, embed);
                            }
                        }
                    }
                }
            }
        }

        public async Task Start()
        {
            try
            {
                ShowJoinAndPartMessages = bool.Parse(_configuration.GetSection("ShowBotJoinMessages").Value);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse ShowBotJoinMessages. Using false.");
                ShowJoinAndPartMessages = false;
            }

            var settings = await _discordBotSettingsRepository.Get();
            if(settings == null)
            {
                settings = GetTokenFromConsole();
            }

            try
            {
                await _client.LoginAsync(TokenType.Bot, settings.Token);
            }
            catch (Discord.Net.HttpException ex)
            {
                if(ex.Reason == "401: Unauthorized")
                {
                    _logger.LogCritical("Token is not correct!");
                    Console.WriteLine("\nToken is incorrect.");
                    Console.Write("Enter Token: ");
                    settings.Token = Console.ReadLine();

                    await _discordBotSettingsRepository.EditAsync(settings);
                    Start();
                }
                else
                {
                    _logger.LogCritical(ex, "An unhandeled HttpException has occured!");
                    Console.WriteLine("An unhandeled HttpException has occured!");
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);

                    Console.WriteLine("Discord Bot is quiting!");

                    if (LavaLinkHelper.isLavaLinkRunning())
                    {
                        LavaLinkHelper.StopLavaLink();
                    }

                    Environment.Exit(0);
                }
            }

            await _client.StartAsync();
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _serviceProvider);
        }

        private DiscordBotSettings GetTokenFromConsole()
        {
            Console.WriteLine("\nToken has not yet been saved.");
            Console.Write("Please enter the bot's token: ");
            var token = Console.ReadLine();

            DiscordBotSettings settings = new DiscordBotSettings { Token = token, Game = "https://github.com/JoyfulReaper" };
            _discordBotSettingsRepository.AddAsync(settings);

            return settings;
        }
    }
}
