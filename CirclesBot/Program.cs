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
    class CallAcceptEmote : IEmote
    {
        public string Name => "✅";
    }

    class CallDenyEmote : IEmote
    {
        public string Name => "❌";
    }

    /// <summary>
    /// TODO LIST
    /// 
    /// 1. Fix this stupid mess
    /// 2. Implement interfaces and call the IModules
    /// 3. Abstract away boilerplate code that keeps getting repeated
    /// 4. Add Leveling system with runescape XP Curves because why not, i like runescape :)
    /// 5. Fix Pages.cs and PagesHandler.cs mess and make them the default for sending embeds for easier use
    /// 
    /// </summary>
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

            CommandHandler.AddCommand("Convert binary number to decimal", (sMsg, buffer) => {
                int val = Convert.ToInt32(buffer.GetRemaining(), 2);
                sMsg.Channel.SendMessageAsync($"**{val}**");
                            
            }, ">b", ">binary");

            CommandHandler.AddCommand("debug", (sMsg, buffer) => {
                if (sMsg.Author.Id == BotOwnerID)
                {
                    int time = Utils.Benchmark(() =>
                    {
                        BeatmapManager.LoadAllMaps();
                    });
                    Logger.Log($"Loaded all maps and it took: {time} milliseconds", LogLevel.Success);
                }
            }, ">load", ">stress");

            CommandHandler.AddCommand("Convert decimal number to binary", (sMsg, buffer) => {
                string binary = Convert.ToString(int.Parse(buffer.GetRemaining()), 2);
                sMsg.Channel.SendMessageAsync($"**{binary}**");

            }, ">d", ">decimal");

            CommandHandler.AddCommand("Convert binary to chars", (sMsg, buffer) => {
                var list = new System.Collections.Generic.List<Byte>();
                string binary = buffer.GetRemaining();

                binary = binary.Replace("_", "");

                for (int i = 0; i < binary.Length; i += 8)
                {
                    try
                    {
                        String t = binary.Substring(i, 8);

                        list.Add(Convert.ToByte(t, 2));
                    }
                    catch { }
                }

                string output = System.Text.Encoding.ASCII.GetString(list.ToArray());

                if(output.Contains("@everyone"))
                    sMsg.Channel.SendMessageAsync($"no");
                else
                    sMsg.Channel.SendMessageAsync($"**{output}**");

            }, ">charstobin", ">cbinary", ">char", ">chars", ">binarytostring", ">cb", ">string");

            CommandHandler.AddCommand("Convert hex to decimal", (sMsg, buffer) => {
                int val = Convert.ToInt32(buffer.GetRemaining(), 16);//int.Parse(buffer.GetRemaining(), NumberStyles.HexNumber | NumberStyles.AllowHexSpecifier);
                sMsg.Channel.SendMessageAsync($"**{val}**");

            }, ">h", ">hex");

            CommandHandler.AddCommand("Make the bot say whatever", async (sMsg, buffer) => {
                //bool delete = buffer.HasParameter("-d");

                string msg = sMsg.Content.Remove(0, 4);
                if (msg.Contains("@everyone"))
                    sMsg.Channel.SendMessageAsync($"no");
                else
                    sMsg.Channel.SendMessageAsync($"**{msg}**");
            }, ">say");

            CommandHandler.AddCommand("ooga booga", async (sMsg, buffer) => {
                EmbedBuilder builder = new EmbedBuilder();
                builder.WithAuthor($"{sMsg.Author.Username} Is calling from {client.GetGuild(sMsg).Name}", $"{sMsg.Author.GetAvatarUrl()}");
                builder.WithThumbnailUrl($"{client.GetGuild(sMsg).IconUrl}");
                builder.Description = "Do you want to pick up??";

                var msgSend = await sMsg.Channel.SendMessageAsync("", false, builder.Build());
                msgSend.AddReactionsAsync(new IEmote[] { new CallAcceptEmote(), new CallDenyEmote() });
            }, ">call");

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
                desc += $"Ram Usage: **{(Process.GetCurrentProcess().PrivateMemorySize64 / 1048576.0).ToString("F")} MB**\n";
                desc += $"GC: **0:** `{GC.CollectionCount(0)}` **1:** `{GC.CollectionCount(1)}` **2:** `{GC.CollectionCount(2)}`\n";
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


            string[] randomQuirkyResponses = new string[] {
                "I agree!", "Thats stupid", "Please tell us more",
                "I sense a lie", "The person above me is speaking straight facts",
                "No cap", "Haha", "Funny", "Error", "True", ":sunglasses:",
                "This is not true", "I laughed", /*"What did the cat say to the mouse?\n||Nothing cats can't speak. Idiot||",*/
                //Low effort
                "727", "dab", "kappa", "lol", "lmao", "owo", "uwu", "yep", "cool", "nice play", "nice cock",
                "i rate 1/10", "i rate 2/10", "i rate 3/10","i rate 4/10","i rate 5/10","i rate 6/10","i rate 7/10", "i rate 8/10", "i rate 9/10", "i rate 10/10",
                "you lost", "cock", "penis", "bye", "hello", "calm down", "it burns", "for real tho", "bro", "i guess",
                ":(", ":)", ":D", ":-)", ":-(", "D:", ";(", ";)", ":o", ":O", ">:O", ":c", "c:", "<3", "</3"
            };

            client.MessageReceived += (s) =>
            {
                if(Utils.GetRandomNumber(1, 100) == 50)
                {
                    s.Channel.SendMessageAsync(randomQuirkyResponses[Utils.GetRandomNumber(0, randomQuirkyResponses.Length - 1)]);
                }

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
                client.SetGameAsync(">help");
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
