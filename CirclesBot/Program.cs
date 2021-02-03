using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace CirclesBot
{
    class Program
    {
        public const ulong BotOwnerID = 591339926017146910;

        static void Main(string[] args)
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            DiscordSocketClient client = new DiscordSocketClient();

            bool signalKill = false;

            //"Module" Initialization
            OsuModule plugin = new OsuModule();


            CommandHandler.AddCommand("Shows bot info", (sMsg, buffer) =>
            {
                var runtimeVer = RuntimeInformation.FrameworkDescription;

                EmbedBuilder embed = new EmbedBuilder();

                embed.WithTitle("Bot Runtime Info");
                string desc = "";

                desc += "[Github Link](https://github.com/AlexCatDev/CirclesBot)\n";

                desc += $"Runtime: **{runtimeVer}**\n";
                desc += $"OS: **{RuntimeInformation.OSDescription} {RuntimeInformation.ProcessArchitecture}**\n";
                desc += $"CPU Cores: **{Environment.ProcessorCount}**\n";
                desc += $"Oppai Version: **{EZPP.GetVersion()}**\n";
                desc += $"Ping: **{client.Latency} MS**\n";
                desc += $"Cached Beatmaps: **{BeatmapManager.CachedMapCount}**\n";

                embed.WithDescription(desc);
                embed.WithColor(Color.Blue);
                sMsg.Channel.SendMessageAsync("", false, embed.Build());
            }, ">info");

            CommandHandler.AddCommand("Shows this embed", (sMsg, buffer) =>
            {
                Pages commandPages = new Pages();

                string desc = "";

                EmbedBuilder eb = new EmbedBuilder();
                eb.WithAuthor("Commands available");

                for (int i = 0; i < CommandHandler.Commands.Count; i++)
                {
                    string tempDesc = "";
                    var currentCommand = CommandHandler.Commands[i];
                    string triggerText = "";
                    for (int j = 0; j < currentCommand.Triggers.Count; j++)
                    {
                        bool isLast = j == currentCommand.Triggers.Count - 1;
                        triggerText += currentCommand.Triggers[j];
                        if (!isLast)
                            triggerText += ", ";
                    }
                    tempDesc += $"**{i + 1}.** {triggerText} **{currentCommand.Description}**\n";

                    if (desc.Length + tempDesc.Length < 2048)
                        desc += tempDesc;
                    else
                    {
                        eb.WithDescription(desc);
                        commandPages.AddContent(eb);

                        eb = new EmbedBuilder();
                        eb.WithAuthor("Commands available");

                        desc = "";
                        desc += tempDesc;
                    }
                }

                eb.WithDescription(desc);
                commandPages.AddContent(eb);


                //eb.WithDescription(desc);
                //eb.WithColor(Color.Green);
                PagesHandler.SendPages(sMsg.Channel, commandPages);
            }, ">help");


            client.MessageReceived += (s) =>
            {
                if (s.Content.ToLower() == "::kys" && s.Author.Id == BotOwnerID)
                {
                    s.Channel.SendMessageAsync("Okay kammerat :ok_hand:").GetAwaiter().GetResult();
                    signalKill = true;
                    return Task.Delay(0);
                }

                if (s.Author.IsBot)
                    return Task.Delay(0);

                Logger.Log(s.Channel.Name + "->" + s.Author.Username + ": " + s.Content);
                try
                {
                    CommandHandler.Handle(s);
                }
                catch(Exception ex) {
                    Logger.Log($"An exception has been thrown when trying to handle a command!", LogLevel.Error);
                    Logger.Log($"User responsable: [{s.Author.Username}]@[{s.Author.Id}] What they wrote: [{s.Content}]\n", LogLevel.Info);
                    Logger.Log($"[Exception Message]\n{ex.Message}\n", LogLevel.Error);
                    Logger.Log($"[Exception Stacktrace]\n{ex.StackTrace}", LogLevel.Warning);
                }
                return Task.Delay(0);
            };

            client.Ready += () =>
            {
                Logger.Log($"[Bot Connect]\nUser={client.CurrentUser.Username}", LogLevel.Success);
                return Task.Delay(0);
            };

            client.Disconnected += (s) =>
            {
                Logger.Log($"[Bot Disconnected]\n{s.Message}", LogLevel.Error);
                return Task.Delay(0);
            };

            client.ReactionAdded += (s, e, x) =>
            {
                if (x.User.Value.IsBot)
                    return Task.Delay(0);

                var msg = s.GetOrDownloadAsync().Result;

                PagesHandler.Handle(msg, x);

                return Task.Delay(0);
            };

            client.LoginAsync(TokenType.Bot, Credentials.DISCORD_API_KEY);
            client.StartAsync();

            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (true)
            {
                Thread.Sleep(10);
                if (signalKill)
                {
                    client.LogoutAsync().GetAwaiter().GetResult();
                    Logger.Log("logged out bye, press enter to continue", LogLevel.Warning);
                    Console.ReadLine();
                }
            }

        }
    }
}
