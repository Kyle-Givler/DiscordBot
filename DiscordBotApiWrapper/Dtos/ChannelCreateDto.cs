﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBotApiWrapper.Dtos
{
    public  class ChannelCreateDto
    {
        public ulong ChannelId { get; set; }
        public string ChannelName { get; set; }
    }
}
