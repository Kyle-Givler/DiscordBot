﻿/*

PLEASE NOTE THIS FILE MAY CONTAIN PROFANITY
Procede at your own risk if you care

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

using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Discord.WebSocket;
using DiscordBotLib.DataAccess;
using DiscordBotLib.Enums;
using DiscordBotLib.Models;
using DiscordBotLib.Services;
using Serilog;

namespace DiscordBotLib.Helpers
{
    public class ProfanityHelper
    {
        public static IProfanityRepository ProfanityRepository = null;

        private static readonly string[] _globalAllowedWords = new[] { "butt", "poop", "bender", "bugger", "bum", "drunk", "dummy", "foobar", "gays", "hardcore", "hookah", "hun", "lesbian", "lesbians",
            "piss", "pissed", "porn", "pornography", "potty", "prod", "psycho", "pube", "pubes", "pubic", "queer", "reefer", "sex", "sexual", "stoned", "suck", "sucks",
            "sucking", "sucked", "tampon", "tart", "testical", "testicle", "thrust", "thug", "tit", "toke", "toots", "topless", "trashy", "turd", "ugly", "urine",
            "balls", "barf", "big breasts", "big black", "big tits", "bloody", "boob", "boobs", "bong", "booger", "boong", "booze", "bummer", "cervix", "climax", 
            "condom", "crack", "crap", "crappy", "dirty", "erect", "erotic", "fart", "fubar", "ganja", "genitals", "god", "hemp", "jerk", "labia", "lmao", "lmfao",
            "maxi", "meth", "moron", "nipple", "nipples", "omg", "opiate", "opium", "organ", "orally", "orgasm", "pcp", "panty", "pee", "penetrate", "penetration",
            "penis", "queers", "rectal", "rectum", "taste my", "teste", "testes", "tied up", "undies", "unwed", "urinal", "vagina", "virgin", "vomit", "vodka",
            "vulva", "wazoo", "weed", "weiner", "wedgie", "whiz", "womb", "rum", "kill", "murder", "stupid", "flaps", "hell", "darn"};

        // I'm not sure why, but words with symbols (*, !) don't seem to work
        // TODO: Look into another filtering library, writing my own filter, or improving the existing solution some how
        private static readonly string[] _globalBannedWords = new[] { "fck", "shat", "sh!t", "sht" }; 

        internal static async Task<bool> ContainsProfanity(Server server, string sentence)
        {
            var filter = await GetProfanityFilterForServer (server);

            return filter.ContainsProfanity(sentence);
        }

        internal async static Task HandleProfanity(SocketUserMessage message, Server server, IApiService apiService)
        {
            var checkString = message.Content.Replace(".", String.Empty).Replace('!', 'i').Replace("-", String.Empty).Replace("*", String.Empty);
            var badWords = await GetProfanity(server, checkString);

            if (badWords.Count > 0)
            {
                var filter = await GetProfanityFilterForServer(server);

                var channel = message.Channel as SocketGuildChannel;
                var guild = channel.Guild;
                var loggingChannel = guild.GetChannel(server.LoggingChannel);
                var badWordsJoined = String.Join(", ", badWords);

                await message.DeleteAsync();

                if (loggingChannel != null)
                {
                    await (loggingChannel as SocketTextChannel).SendLogAsync("Profanity Filter", $"{message.Author.Mention} said a bad word: {message.Content}\nin {channel.Guild.Name}/{channel.Name}.\nWords: `{badWordsJoined}`",
                        ColorHelper.GetColor(server), apiService, ImageLookupUtility.GetImageUrl("LOGGING_IMAGES"));
                }

                if (server.ProfanityFilterMode == ProfanityFilterMode.FilterCensor)
                {
                    var censored = filter.CensorString(checkString);
                    await (channel as SocketTextChannel).SendMessageAsync($"{message.Author.Mention}, please don't swear. {message.Author.Username}'s Censored message:\n{censored.Replace("*", "#")}");
                }
                else
                {
                    await (channel as SocketTextChannel).SendMessageAsync($"{message.Author.Mention}, please don't swear.");
                }
            }
        }

        public static async Task<ReadOnlyCollection<string>> GetProfanity(Server server, string sentence)
        {
            var filter = await GetProfanityFilterForServer(server);
            return filter.DetectAllProfanities(sentence);
        }

        public static async Task<ProfanityFilter.ProfanityFilter> GetProfanityFilterForServer(Server server)
        {
            // TODO Look into caching this some how...
            // TODO Look into caching in general (Cache servers/guilds also for example)
            // Looks like MonkeyCache just uses a SQLite db to caches and that is what we are doing anyway
            // Maybe do some kind of in memeory caching?
            if (ProfanityRepository == null)
            {
                Log.Warning("ProfanityRepository is null!");
                throw new InvalidOperationException("ProfanityRepostiry cannot be null, please set it before using the ProfanityHelper!");
            }

            ProfanityFilter.ProfanityFilter filter = new ProfanityFilter.ProfanityFilter();
            filter.RemoveProfanity(_globalAllowedWords);
            filter.AddProfanity(_globalBannedWords);

            var allowedWords = await ProfanityRepository.GetAllowedProfanity(server.GuildId);
            var blockedWord = await ProfanityRepository.GetBlockedProfanity(server.GuildId);

            foreach (var word in allowedWords)
            {
                filter.RemoveProfanity(word.Word);
            }

            foreach (var word in blockedWord)
            {
                filter.AddProfanity(word.Word);
            }

            return filter;
        }
    }
}
