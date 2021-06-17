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

using DiscordBotApiWrapper.Dtos;
using DiscordBotApiWrapper.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace DiscordBotApiWrapper
{
    public class ServerLogItemApi : IServerLogItemApi
    {
        private readonly IApiClient _client;

        public ServerLogItemApi(IApiClient client)
        {
            _client = client;
        }

        public async Task<HttpStatusCode> DeleteServerLogItem(int id)
        {
            return await _client.DeleteAsync($"/api/ServerLogItems/{id}");
        }

        public async Task<IEnumerable<ServerLogItem>> GetServerLogItemsForGuild(int guildId)
        {
            return await _client.GetAsync<IEnumerable<ServerLogItem>>($"/api/ServerLogItems/GuildId/{guildId}");
        }

        public async Task<IEnumerable<ServerLogItem>> GetServerLogItemsForGuild(int guildId, int page)
        {
            return await _client.GetAsync<IEnumerable<ServerLogItem>>($"/api/ServerLogItems/GuildId/{guildId}?page={page}");
        }

        public async Task<ServerLogItem> GetServerLogItem(int id)
        {
            return await _client.GetAsync<ServerLogItem>($"/api/ServerLogItems/{id}");
        }

        public async Task<HttpStatusCode> SaveServerLogItem(ServerLogItemCreateDto item)
        {
            var statusCode = await _client.PostAsync<ServerLogItemCreateDto>("/api/ServerLogItems/", item);

            if(statusCode == HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedAccessException("Your username or password is incorrect!");
            }

            return statusCode;
        }
    }
}
