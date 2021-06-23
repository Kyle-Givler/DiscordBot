DiscordBot
==========
A Discord Bot written in C# using the Discord.NET library. I plan to continue to expand this bot with common Discord bot features. If you would like to see a command/feature/different database or ORM/etc please open an issues, I love a challenge :)

If you would like to contribute, offer suggestion/criticism or hang out on the Discord server I test the bot in join here: https://discord.gg/2bxgHyfN6A If you want to invite the bot to your server then ask in the server linked above. For now it's just running on my PC as I develop it though.

DiscordBot has the following features:

* Auto Roles: Allow Discord roles to be registered with the bot, these registered roles will be given to all members when joining the server.
  * autoroles, addautotole, delautorole, runautorules
* Fun: 
  * 8ball, coinflip, rolldie, RussianRoulette, lmgtfy (Let Me Google That for You), RockPaperScissors, giphy, random
* General commands including:
  * Math, ping, about, owner, echo, info, server, image, help, uptime, servers
* Moderation commands including:
  * Ability to enable or disable sending invites to other server (serverinvites)
  * Ability to censor or delete messages with profanity, and allow or disallow words. This is pretty basic and doesn't take too much creativity to bypass :(
  * ban, unban, kick, purge, prefix, welcome, mute, unmute, slowmode, logs, embedcolor, profanityallow, profanityblock, profanityfilter
* Music Commands
  * search, play, join, skip, pause, resume
* Ranks: Allows discord roles to be registered with the bot as ranks, users can assign these ranks to themselves
  * ranks, addrank, delrank, rank
* Reddit command
  * Show a post from a chosen or random subreddit (reddit) delete known subreddit (subredditremove)
  * subredditlearning can be turned 'on' or 'off' and requires the redditor role to teach the bot new subreddits
* Timzone features:
  * registertimezone, userstime, time (get the time in any timezone), validtimezone (validate a timezone)
* Web
  * google (search google), youtube (search youtube)
* Note Commands
   * note create, note list, note delete, note show

DiscordApi is an API that DiscordBot can integrate with to report log events per server, and command usage. DiscordBotApiWrapper is an API wrapper that makes this intergration easier. The next step is to construct a website to show log events and command usage statistics.

This bot uses Dapper as the ORM and currently uses SQLite as the database engine. 
If you find this bot useful, but would prefer a different database engine please let me know as I would like to look into adding support for other databases later.

Technologies used in the making of this bot: C#, Discord.NET, Serilog, Dapper, Entity Framework Core, SQL Server, SQLite, ASP.NET Core Web API

Notable Nuget Packages Used: Victora, Profanity.Detector, TimezoneConverter, Discord.Addons.Interactive 