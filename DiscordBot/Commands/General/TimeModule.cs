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

using Discord.Commands;
using Discord.WebSocket;
using DiscordBotLib.DataAccess;
using DiscordBotLib.Models;
using DiscordBotLib.Helpers;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using TimeZoneConverter;
using DiscordBotLib.Services;
using Discord.Rest;

// TODO Add logging

namespace DiscordBot.Commands
{
    [Name("Time")]
    public class TimeModule : ModuleBase<SocketCommandContext>
    {
        private readonly ILogger<TimeModule> _logger;
        private readonly IUserTimeZonesRepository _userTimeZones;
        private readonly IServerService _serverService;
        private readonly IUserRepository _userRepository;

        public TimeModule(ILogger<TimeModule> logger,
            IUserTimeZonesRepository userTimeZones,
            IServerService serverService,
            IUserRepository userRepository)
        {
            _logger = logger;
            _userTimeZones = userTimeZones;
            _serverService = serverService;
            _userRepository = userRepository;
        }

        [Command("registertimezone")]
        [Alias("registertz", "regtz", "settz", "settimezone")]
        [Summary("Register your timezone with the bot")]
        public async Task RegisterTimeZone([Summary("Your IANA or Windows timezone")][Remainder] string timeZone = null)
        {
            await Context.Channel.TriggerTypingAsync();

            _logger.LogInformation("{username}#{discriminator} executed registertimezone ({timezone}) on {server}/{channel}",
                Context.User.Username, Context.User.Discriminator, timeZone, Context.Guild?.Name ?? "DM", Context.Channel.Name);

            if (await ServerHelper.CheckIfContextIsDM(Context))
            {
                return;
            }

            if (timeZone == null)
            {
                await Context.Channel.SendEmbedAsync("Provide a Time Zone", "Please provide a valid windows or IANA timezone.", 
                    ColorHelper.GetColor(await _serverService.GetServer(Context.Guild)));
                return;
            }

            if (!TryParseTimeZone(timeZone, out TimeZoneInfo timeZoneInfo))
            {
                await Context.Channel.SendEmbedAsync("Invalid Time Zone", "Please provide a valid windows or IANA timezone.",
                    ColorHelper.GetColor(await _serverService.GetServer(Context.Guild)));
            }
            else
            {
                var user = await _userRepository.GetByUserId(Context.User.Id);

                if(user == null)
                {
                    await _userRepository.AddAsync(new User { UserId = Context.User.Id, UserName = Context.User.Username });
                    user = await _userRepository.GetByUserId(Context.User.Id);
                }

                var userTimeZone = await _userTimeZones.GetByUserID(Context.User.Id);

                if (userTimeZone == null)
                {
                    if(user == null)
                    {
                        await _userRepository.AddAsync(new User { UserId = Context.User.Id, UserName = Context.User.Username });
                    }

                    var userTz = new UserTimeZone
                    {
                        UserId = user.Id,
                        TimeZone = timeZone,
                    };

                    await _userTimeZones.AddAsync(userTz);
                    await Context.Channel.SendEmbedAsync("Succesfully Registered", $"Successfully registered your time zone: `{timeZoneInfo.DisplayName}`",
                        ColorHelper.GetColor(await _serverService.GetServer(Context.Guild)));
                }
                else
                {
                    userTimeZone.TimeZone = timeZone;
                    await _userTimeZones.EditAsync(userTimeZone);
                    await Context.Channel.SendEmbedAsync("Succesfully Updated", $"Successfully updated your time zone: `{timeZoneInfo.DisplayName}`",
                        ColorHelper.GetColor(await _serverService.GetServer(Context.Guild)));
                }
            }
        }

        [Command("userstime")]
        [Alias("usertime", "utime")]
        [Summary("Get the time for a given user")]
        public async Task UserTime([Summary("The user to get the time and date for")][Remainder] SocketUser user = null)
        {
            if (user == null)
            {
                user = Context.User;
            }

            await Context.Channel.TriggerTypingAsync();

            _logger.LogInformation("{username}#{discriminator} executed userstime ({user}) on {server}/{channel}",
                Context.User.Username, Context.User.Discriminator, user.Username, Context.Guild?.Name ?? "DM", Context.Channel.Name);

            var userDb = await _userRepository.GetByUserId(user.Id);
            if(userDb == null)
            {
                userDb = new User
                {
                    UserId = user.Id,
                    UserName = user.Username
                };
                await _userRepository.AddAsync(userDb);
            }

            var userTimeZone = await _userTimeZones.GetByUserID(user.Id);
            if(userTimeZone == null)
            {
                await Context.Channel.SendEmbedAsync("Not Registered", $"{user.Username} has not registered their time zone.",
                    ColorHelper.GetColor(await _serverService.GetServer(Context.Guild)));

                return;
            }

            TimeZoneInfo tzi;
            if (!TryParseTimeZone(userTimeZone.TimeZone, out tzi))
            {
                await Context.Channel.SendEmbedAsync("Invalid Time Zone", "User some how registered an invalid timezone...",
                    ColorHelper.GetColor(await _serverService.GetServer(Context.Guild)));

                return;
            }

            DateTime time = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tzi);
            await Context.Channel.SendEmbedAsync($"{user.Username}'s Time", $"The date and time for {user.Username} is:\n`{time}`",
                ColorHelper.GetColor(await _serverService.GetServer(Context.Guild)));
        }

        [Command("validtimezone")]
        [Alias("validtz")]
        [Summary("Validate a windows or IANA timezone")]
        public async Task ValidTimeZone([Summary("The time zone to validate")][Remainder] string timeZone = null)
        {
            await Context.Channel.TriggerTypingAsync();

            _logger.LogInformation("{username}#{discriminator} executed validtimezone ({timezone}) on {server}/{channel}",
                Context.User.Username, Context.User.Discriminator, timeZone, Context.Guild?.Name ?? "DM", Context.Channel.Name);

            if (timeZone == null)
            {
                await Context.Channel.SendEmbedAsync("Unable to parse time zone", "Please provide a windows or IANA timezone",
                    ColorHelper.GetColor(await _serverService.GetServer(Context.Guild)));

                return;
            }

            TimeZoneInfo tzi;
            if (!TZConvert.TryGetTimeZoneInfo(timeZone, out tzi))
            {
                await Context.Channel.SendEmbedAsync("Invalid Time Zone", $"`{timeZone}` is *not* a valid windows or IANA timezone.",
                    ColorHelper.GetColor(await _serverService.GetServer(Context.Guild)));

                return;
            }
            else
            {
                await Context.Channel.SendEmbedAsync("Valid Time Zone", $"`{timeZone}` *is* a valid windows or IANA timezone.",
                    ColorHelper.GetColor(await _serverService.GetServer(Context.Guild)));
            }
        }

        [Command("time")]
        [Summary("Get the time in the given timezone")]
        public async Task GetTime([Summary("The IANA or Windows time zone to get the time for")][Remainder] string timeZone = null)
        {
            await Context.Channel.TriggerTypingAsync();

            _logger.LogInformation("{username}#{discriminator} executed time ({timezone}) on {server}/{channel}",
                Context.User.Username, Context.User.Discriminator, timeZone, Context.Guild?.Name ?? "DM", Context.Channel.Name);

            //if (timeZone == null)
            //{
            //    await Context.Channel.SendEmbedAsync("Unable to parse time zone", "Please provide a windows or IANA timezone",
            //        ColorHelper.GetColor(await _serverService.GetServer(Context.Guild)));

            //    //return;
            //}

            if(timeZone != null && TryParseTimeZone(timeZone, out TimeZoneInfo tzi))
            {
                DateTime time = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tzi);
                await Context.Channel.SendEmbedAsync("Current Time", $"The current time in {timeZone} is:\n`{time}`",
                    ColorHelper.GetColor(await _serverService.GetServer(Context.Guild)));

                return;
            }
            else
            {
                await Context.Channel.SendEmbedAsync("Invalid Time Zone", $"{timeZone} is *not* a valid windows or IANA timezone.",
                    ColorHelper.GetColor(await _serverService.GetServer(Context.Guild)));
            }

            //TimeZoneInfo tzi;
            //if (!TZConvert.TryGetTimeZoneInfo(timeZone, out tzi))
            //{
            //    await Context.Channel.SendEmbedAsync("Invalid Time Zone", $"{timeZone} is *not* a valid windows or IANA timezone.",
            //        ColorHelper.GetColor(await _serverService.GetServer(Context.Guild)));
            //}
            //else
            //{
            //    DateTime time = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tzi);
            //    await Context.Channel.SendEmbedAsync("Current Time", $"The current time in {timeZone} is:\n`{time}`",
            //        ColorHelper.GetColor(await _serverService.GetServer(Context.Guild)));
            //}

            RestGuildUser user = null;
            if (timeZone != null)
            {
                user = (await Context.Guild.SearchUsersAsync(timeZone, 1)).FirstOrDefault();
            }

            if (user == null)
            {
                user = (await Context.Guild.SearchUsersAsync(Context.User.Username)).FirstOrDefault();
                if(user == null)
                {
                    await ReplyAsync("Couldn't find you....");
                    return;
                }
            }
            var server = await _serverService.GetServer(Context.Guild);
            var prefix = String.Empty;
            if (server != null)
            {
                prefix = server.Prefix;
            }

            await ReplyAsync($"The command you are looking for is `{prefix}utime`, but I helped you out anyway!");
            await UserTime(Context.Guild.GetUser(user.Id));

        }

        private bool TryParseTimeZone(string timeZone, out TimeZoneInfo tzi)
        {
            tzi = null;
            if (!TZConvert.TryGetTimeZoneInfo(timeZone, out tzi))
            {
                foreach(string tz in TZConvert.KnownIanaTimeZoneNames)
                {
                    if(tz.ToLowerInvariant().Contains(timeZone.ToLowerInvariant()))
                    {
                        tzi = TZConvert.GetTimeZoneInfo(tz);
                        return true;
                    }
                }

                foreach (string tz in TZConvert.KnownWindowsTimeZoneIds)
                {
                    if (tz.ToLowerInvariant().Contains(timeZone.ToLowerInvariant()))
                    {
                        tzi = TZConvert.GetTimeZoneInfo(tz);
                        return true;
                    }
                }
                return false;
            }

            return true;
        }
    }
}
