﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace CirclesBot
{
    /// <summary>
    /// TODO LIST
    /// 
    /// 1. Fix this stupid mess
    /// 2. Implement interfaces and call the IModules - Kinda done
    /// 3. Abstract away boilerplate code that keeps getting repeated
    /// 4. Add Leveling system with runescape XP Curves because why not, i like runescape :)
    /// 5. Fix Pages.cs and PagesHandler.cs mess and make them the default for sending embeds for easier use
    /// 
    /// </summary>
    class Program : Module
    {
        public override string Name => "Main Module";

        public static readonly string[] RandomQuirkyResponses = new string[] {
                "I agree!", "Thats stupid", "Please tell us more",
                "The person above me is speaking straight facts",
                "No cap", "Haha", "Funny", "Error", "True", ":sunglasses:",
                "This is not true", "I laughed",
                "727", "dab", "kappa", "lol", "lmao", "owo", "uwu", "yep", "cool", "nice play", "nice cock",
                "i rate 1/10", "i rate 2/10", "i rate 3/10","i rate 4/10","i rate 5/10","i rate 6/10","i rate 7/10", "i rate 8/10", "i rate 9/10", "i rate 10/10",
                "you lost", "cock", "penis", "bye", "hello", "calm down", "it burns", "for real tho", "bro", "i guess",
                ":(", ":)", ":D", ":-)", ":-(", "D:", ";(", ";)", ":o", ":O", ">:O", ":c", "c:", "<3", "</3"
            };

        public const ulong BotOwnerID = 591339926017146910;

        public static DiscordSocketClient Client = new DiscordSocketClient(new DiscordSocketConfig() { AlwaysDownloadUsers = true, ConnectionTimeout = Int32.MaxValue });

        public static List<Module> LoadedModules = new List<Module>();

        private static Stopwatch uptimeWatch = new Stopwatch();
        private static Stopwatch downtimeWatch = new Stopwatch();

        public static Credentials Credentials;

        public static int GetMemberCount()
        {
            int total = 0;
            foreach (var guild in Client.Guilds)
            {
                total += guild.MemberCount;
            }
            return total;
        }

        public static T GetModule<T>() where T : Module
        {
            foreach (var module in LoadedModules)
            {
                if (module is T t)
                    return t;
            }

            return null;
        }

        public Program()
        {
            Commands.Add(new Command("Enable a command", (sMsg, buffer) => {
                string commandToEnable = buffer.GetRemaining();

                if (sMsg.Author.Id == BotOwnerID)
                {
                    foreach (var module in LoadedModules)
                    {
                        foreach (var command in module.Commands)
                        {
                            if (command.Triggers.Contains(commandToEnable))
                            {
                                command.IsEnabled = true;
                                sMsg.Channel.SendMessageAsync($"Enabled **{commandToEnable}** in `{module.Name}`");
                                return;
                            }
                        }
                    }
                    sMsg.Channel.SendMessageAsync($"{commandToEnable} no such command found");
                }
            }, ">enable"));

            Commands.Add(new Command("Disable a command", (sMsg, buffer) => {
                string commandToDisable = buffer.GetRemaining();

                if (commandToDisable == ">disable" || commandToDisable == ">enable")
                    return;

                if (sMsg.Author.Id == BotOwnerID)
                {
                    foreach (var module in LoadedModules)
                    {
                        foreach (var command in module.Commands)
                        {
                            if (command.Triggers.Contains(commandToDisable))
                            {
                                command.IsEnabled = false;
                                sMsg.Channel.SendMessageAsync($"Disabled **{commandToDisable}** in `{module.Name}`");
                                return;
                            }
                        }
                    }
                    sMsg.Channel.SendMessageAsync($"{commandToDisable} no such command found");
                }
            }, ">disable"));

            Commands.Add(new Command("Shows bot info", (sMsg, buffer) =>
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
                desc += $"Ping: **{Client.Latency} MS**\n";
                desc += $"Uptime: **{Utils.FormatTime(uptimeWatch.Elapsed).Replace("Ago", "")}**\n";
                desc += $"Downtime: **{Utils.FormatTime(downtimeWatch.Elapsed).Replace("Ago", "")}**\n";
                desc += $"Guilds: **{Client.Guilds.Count}**\n";
                desc += $"TotalMembers: **{GetMemberCount()}**\n";
                desc += $"Modules: **{LoadedModules.Count}**\n";

                embed.WithDescription(desc);
                embed.WithColor(Color.Blue);
                //embed.WithFooter($"Started {Utils.FormatTime(uptimeWatch.Elapsed)}");
                sMsg.Channel.SendMessageAsync("", false, embed.Build());
            }, ">info"));

            //This is very ugly
            Commands.Add(new Command("Shows this embed", (sMsg, buffer) =>
            {
                Pages commandPages = new Pages();

                string desc = "";

                EmbedBuilder eb = new EmbedBuilder();
                eb.WithAuthor("Commands available");
                foreach (var module in LoadedModules)
                {
                    string tempDesc = "";
                    tempDesc += $"`{module.Name}`\n";
                    for (int i = 0; i < module.Commands.Count; i++)
                    {
                        var currentCommand = module.Commands[i];
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
                        {
                            desc += tempDesc;
                            tempDesc = "";
                        }
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
                }

                eb.WithDescription(desc);
                commandPages.AddContent(eb);


                //eb.WithDescription(desc);
                //eb.WithColor(Color.Green);
                PagesHandler.SendPages(sMsg.Channel, commandPages);
            }, ">help"));
        }


        static void Main(string[] args)
        {
            if (!File.Exists("./Credentials.txt"))
            {
                Logger.Log("No credentials, please put your credentials into Credentials.txt. Press enter when you have done that.");
                File.WriteAllText("./Credentials.txt", JsonConvert.SerializeObject(new Credentials()));
                Console.ReadLine();
            }

            Credentials = JsonConvert.DeserializeObject<Credentials>(File.ReadAllText("./Credentials.txt"));

            if(String.IsNullOrEmpty(Credentials.DISCORD_API_KEY))
            {
                Logger.Log("You need to enter a discord api key into the Credentials.txt file.", LogLevel.Error);
                Console.ReadLine();
                Environment.Exit(0);
            }

            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            Logger.Log("Running bot!");

            bool signalKill = false;

            Logger.Log("Loading Modules", LogLevel.Info);

            int time = Utils.Benchmark(() =>
            {
                foreach (var type in System.Reflection.Assembly.GetExecutingAssembly().GetTypes())
                {
                    if (type.IsInterface || type.IsAbstract)
                    {
                        continue;
                    }
                    else
                    {
                        if (type.IsAssignableTo(typeof(Module)))
                        {
                            Logger.Log($"Found module: {type.Name}");
                            Module loadedModule = (Module)Activator.CreateInstance(type);
                            LoadedModules.Add(loadedModule);
                            Logger.Log($"Loaded module: {type.Name}", LogLevel.Success);
                        }
                    }
                }
            });

            Logger.Log($"Modules took {time} milliseconds to load");

            Client.MessageReceived += (s) =>
            {
                //1% chance
                if (Utils.GetRandomChance(1))
                {
                    s.Channel.SendMessageAsync(RandomQuirkyResponses[Utils.GetRandomNumber(0, RandomQuirkyResponses.Length - 1)]);
                }

                if (s.Content.ToLower() == ">die" && s.Author.Id == BotOwnerID)
                {
                    s.Channel.SendMessageAsync("ok i die").GetAwaiter().GetResult();
                    signalKill = true;
                    return Task.Delay(0);
                }

                if (s.Author.IsBot)
                    return Task.Delay(0);

                Logger.Log(s.Channel.Name + "->" + s.Author.Username + ": " + s.Content);
                try
                {
                    foreach (var module in LoadedModules)
                    {
                        if (s is SocketUserMessage sMsg)
                        {
                            module.Commands.ForEach((command) => command.Handle(sMsg));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"An exception has been thrown when trying to handle a command!", LogLevel.Error);
                    Logger.Log($"User responsable: [{s.Author.Username}]@[{s.Author.Id}] What they wrote: [{s.Content}]\n", LogLevel.Info);
                    Logger.Log($"[Exception Message]\n{ex.Message}\n", LogLevel.Error);
                    Logger.Log($"[Exception Stacktrace]\n{ex.StackTrace}", LogLevel.Warning);
                }
                return Task.Delay(0);
            };

            Client.Ready += () =>
            {
                uptimeWatch.Start();
                downtimeWatch.Stop();
                Logger.Log($"[Bot Connect]\nUser={Client.CurrentUser.Username}", LogLevel.Success);
                Client.SetGameAsync(">help");
                return Task.Delay(0);
            };

            Client.Disconnected += (s) =>
            {
                uptimeWatch.Stop();
                downtimeWatch.Start();
                Logger.Log($"[Bot Disconnected]\n{s.Message}", LogLevel.Error);
                return Task.Delay(0);
            };

            Client.ReactionAdded += (s, e, x) =>
            {
                if (x.User.Value.IsBot)
                    return Task.Delay(0);

                var msg = s.GetOrDownloadAsync().Result;

                PagesHandler.Handle(msg, x);

                return Task.Delay(0);
            };

            Client.ReactionRemoved += (s, e, x) =>
            {
                if (x.User.Value.IsBot)
                    return Task.Delay(0);

                var msg = s.GetOrDownloadAsync().Result;

                PagesHandler.Handle(msg, x);

                return Task.Delay(0);
            };

            Client.GuildAvailable += (s) =>
            {
                return Task.Delay(0);
            };

            Client.LoginAsync(TokenType.Bot, Credentials.DISCORD_API_KEY);
            Logger.Log("Logging in...");
            Client.StartAsync();

            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (true)
            {
                Thread.Sleep(10);
                if (signalKill)
                {
                    Client.LogoutAsync().GetAwaiter().GetResult();
                    Logger.Log("logged out bye, press enter to continue", LogLevel.Warning);
                    Console.ReadLine();
                }
            }
        }
    }
}
