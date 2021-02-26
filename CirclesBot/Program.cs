using System;
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
    /// 2. Abstract away boilerplate code that keeps getting repeated
    /// 3. Fix Pages.cs and PagesHandler.cs mess and make them the default for sending embeds for easier use
    /// 
    /// </summary>
    class Program : Module
    {
        public const string XD =
            ":joy::joy::joy::joy::joy::joy::joy::joy::joy::joy::joy::joy::joy::joy::joy:\n"+
            ":joy::cool::cool::cool::cool::cool::cool::cool::cool::cool::cool::cool::cool::cool::joy:\n" +
            ":joy::cool::100::cool::cool::cool::100::cool::100::100::100::cool::cool::cool::joy:\n" +
            ":joy::cool::100::100::cool::100::100::cool::100::cool::100::100::cool::cool::joy:\n" +
            ":joy::cool::cool::100::cool::100::cool::cool::100::cool::cool::100::100::cool::joy:\n" +
            ":joy::cool::cool::100::100::100::cool::cool::100::cool::cool::cool::100::cool::joy:\n" +
            ":joy::cool::cool::cool::100::cool::cool::cool::100::cool::cool::cool::100::cool::joy:\n" +
            ":joy::cool::cool::100::100::100::cool::cool::100::cool::cool::cool::100::cool::joy:\n" +
            ":joy::cool::cool::100::cool::100::cool::cool::100::cool::cool::100::100::cool::joy:\n" +
            ":joy::cool::100::100::cool::100::100::cool::100::cool::100::100::cool::cool::joy:\n" +
            ":joy::cool::100::cool::cool::cool::100::cool::100::100::100::cool::cool::cool::joy:\n" +
            ":joy::cool::cool::cool::cool::cool::cool::cool::cool::cool::cool::cool::cool::cool::joy:\n" +
            ":joy::joy::joy::joy::joy::joy::joy::joy::joy::joy::joy::joy::joy::joy::joy:\n";

        public static readonly string[] RandomQuirkyResponses = new string[] {
                "I agree!", "Thats stupid", "Please tell us more",
                "The person above me is speaking straight facts",
                "No cap", "Haha", "Funny", "Error", "True", ":sunglasses:",
                "This is not true", "I laughed",
                "727", "dab", "kappa", "lol", "lmao", "owo", "uwu", "yep", "cool", "nice play", "nice cock",
                "i rate 1/10", "i rate 2/10", "i rate 3/10","i rate 4/10","i rate 5/10","i rate 6/10","i rate 7/10", "i rate 8/10", "i rate 9/10", "i rate 10/10",
                "you lost", "cock", "penis", "bye", "hello", "calm down", "it burns", "for real tho", "bro", "i guess",
                ":(", ":)", ":D", ":-)", ":-(", "D:", ";(", ";)", ":o", ":O", ">:O", ":c", "c:", "<3", "</3", ":^)"
        };

        public static DiscordSocketClient Client = new DiscordSocketClient(new DiscordSocketConfig() { AlwaysDownloadUsers = true, ConnectionTimeout = -1 });

        public static List<Module> LoadedModules = new List<Module>();

        private static Stopwatch onlineWatch = new Stopwatch();
        private static Stopwatch offlineWatch = new Stopwatch();

        public static Config Config { get; private set; }

        public static ulong TotalCommandsHandled;

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

        public override string Name => "Main Module";

        public override int Order => 2;

        public Program()
        {
            AddCMD("Enable a command", (sMsg, buffer) => {
                string commandToEnable = buffer.GetRemaining();

                if (sMsg.Author.Id == Config.BotOwnerID)
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
            }, ">enable");

            AddCMD("Disable a command", (sMsg, buffer) => {
                string commandToDisable = buffer.GetRemaining();

                if (commandToDisable == ">disable" || commandToDisable == ">enable")
                    return;

                if (sMsg.Author.Id == Config.BotOwnerID)
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
            }, ">disable");

            AddCMD("Shows bot info", (sMsg, buffer) =>
            {
                var runtimeVer = RuntimeInformation.FrameworkDescription;

                EmbedBuilder embed = new EmbedBuilder();

                embed.WithTitle("Bot Info");
                string desc = "";

                desc += $"[Github Link]({Config.GithubURL})\n";

                desc += $"Runtime: **{runtimeVer}**\n";
                desc += $"OS: **{RuntimeInformation.OSDescription} {RuntimeInformation.ProcessArchitecture}**\n";
                desc += $"CPU Cores: **{Environment.ProcessorCount}**\n";
                desc += $"Ram Usage: **{(Process.GetCurrentProcess().PrivateMemorySize64 / 1048576.0).ToString("F")} MB**\n";
                desc += $"CPU Time: **{Utils.FormatTime(Process.GetCurrentProcess().TotalProcessorTime, ago: false)}**\n";
                desc += $"GC: **0:** `{GC.CollectionCount(0)}` **1:** `{GC.CollectionCount(1)}` **2:** `{GC.CollectionCount(2)}`\n";
                desc += $"Oppai Version: **{EZPP.GetVersion()}**\n";
                desc += $"Ping: **{Client.Latency} MS**\n";
                desc += $"Online-Time: **{Utils.FormatTime(onlineWatch.Elapsed, ago: false)}**\n";
                desc += $"Offline-Time: **{Utils.FormatTime(offlineWatch.Elapsed, ago: false)}**\n";
                desc += $"Serving: **{Client.Guilds.Count} Guilds And {GetMemberCount()} Members**\n";
                desc += $"Commands Handled: **{TotalCommandsHandled}**\n";
                desc += $"Bancho API Calls: **{BanchoAPI.TotalAPICalls}**\n";
                desc += $"Loaded Modules: **{LoadedModules.Count}**\n";

                embed.WithDescription(desc);
                embed.WithColor(Color.Blue);
                sMsg.Channel.SendMessageAsync("", false, embed.Build());
            }, ">info");

            //This is very ugly
            AddCMD("Shows this embed", (sMsg, buffer) =>
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
                            commandPages.AddEmbed(eb.Build());

                            eb = new EmbedBuilder();
                            eb.WithAuthor("Commands available");

                            desc = "";
                            desc += tempDesc;
                        }
                    }
                }

                eb.WithDescription(desc);
                commandPages.AddEmbed(eb.Build());

                PagesHandler.SendPages(sMsg.Channel, commandPages);
            }, ">help");
        }

        static void Main(string[] args)
        {
            if (!File.Exists(Config.Filename))
            {
                Logger.Log($"No config, please put your credentials into {Config.Filename}. Press enter when you have done that.");
                File.WriteAllText(Config.Filename, JsonConvert.SerializeObject(new Config(), Formatting.Indented));
                Console.ReadLine();
            }

            while (true)
            {
                try
                {
                    Config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(Config.Filename));
                    Config.Verify();
                    break;
                }
                catch(Exception ex)
                {
                    Logger.Log($"Error when parsing config: {ex.Message}", LogLevel.Error);
                    Logger.Log("Press enter to try again", LogLevel.Info);
                    Console.ReadLine();
                }
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
                            Logger.Log($"Loading: {type.Name}");
                            Module loadedModule = (Module)Activator.CreateInstance(type);
                            LoadedModules.Add(loadedModule);
                            Logger.Log($"Done", LogLevel.Success);
                        }
                    }
                }

                LoadedModules.Sort((x, y) => x.Order.CompareTo(y.Order));
            });

            Logger.Log($"Modules took {time} milliseconds to load");

            bool isRunning = true;

            Client.MessageReceived += (s) =>
            {
                if (s.Author.IsBot)
                    return Task.Delay(0);

                //This is here so i can more easily run instances of the same bot
                if (s.Content.ToLower() == ">stop" && s.Author.Id == Config.BotOwnerID)
                {
                    isRunning = false;

                    s.Channel.SendMessageAsync("I will no longer handle commands");
                }

                if (s.Content.ToLower() == ">start" && s.Author.Id == Config.BotOwnerID)
                {
                    isRunning = true;

                    s.Channel.SendMessageAsync("I will handle commands again");
                }

                if (s.Content.ToLower() == ">die" && s.Author.Id == Config.BotOwnerID)
                {
                    s.Channel.SendMessageAsync("ok i die").GetAwaiter().GetResult();
                    signalKill = true;
                    return Task.Delay(0);
                }

                if (!isRunning)
                    return Task.Delay(0);
                //1% chance
                if (Utils.GetRandomChance(1))
                {
                    s.Channel.SendMessageAsync(RandomQuirkyResponses[Utils.GetRandomNumber(0, RandomQuirkyResponses.Length - 1)]);
                }

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
                    if (Config.DMOwnerOnError)
                    {
                        string error = $"An exception has been thrown when trying to handle a command!\n";
                        error += $"User: **[{s.Author.Username}#{s.Author.Id}]** said: **[{s.Content}]**\n";
                        error += $"[Exception Message]\n**{ex.Message}**\n";
                        error += $"[Exception Stacktrace]\n{ex.StackTrace}";

                        Client.GetUser(Config.BotOwnerID).SendMessageAsync(error);
                    }

                    Logger.Log($"An exception has been thrown when trying to handle a command!", LogLevel.Error);
                    Logger.Log($"User responsible: [{s.Author.Username}]@[{s.Author.Id}] What they wrote: [{s.Content}]\n", LogLevel.Info);
                    Logger.Log($"[Exception Message]\n{ex.Message}\n", LogLevel.Error);
                    Logger.Log($"[Exception Stacktrace]\n{ex.StackTrace}", LogLevel.Warning);
                }
                return Task.Delay(0);
            };

            Client.Ready += () =>
            {
                onlineWatch.Start();
                offlineWatch.Stop();
                Logger.Log($"[Bot Connect]\nUser={Client.CurrentUser.Username}", LogLevel.Success);
                Client.SetGameAsync(">help");
                return Task.Delay(0);
            };

            Client.Disconnected += (s) =>
            {
                onlineWatch.Stop();
                offlineWatch.Start();
                Logger.Log($"[Bot Disconnected]\n{s.Message}", LogLevel.Error);
                return Task.Delay(0);
            };

            Client.ReactionAdded += async (s, e, x) =>
            {
                if (x.UserId != Client.CurrentUser.Id)
                {
                    PagesHandler.Handle(x.MessageId, x);
                }
            };

            Client.ReactionRemoved += async (s, e, x) =>
            {
                if (x.UserId != Client.CurrentUser.Id)
                {
                    PagesHandler.Handle(x.MessageId, x);
                }
            };

            Client.GuildAvailable += (s) =>
            {
                return Task.Delay(0);
            };

            Client.LoginAsync(TokenType.Bot, Config.DISCORD_API_KEY);
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
                    Environment.Exit(0);
                }
            }
        }
    }
}
