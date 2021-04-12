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
    class CoreModule : Module
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

        private static Stopwatch runTimeWatch = new Stopwatch();

        public static Config Config { get; private set; }

        public static ulong TotalCommandsHandled;

        public static event Action<double> OnUpdate;
        public static event Action<SocketMessage> OnMessageReceived;

        public static bool IgnoreMessages { get; private set; }

        public static int GetMemberCount()
        {
            try
            {
                int total = 0;
                foreach (var guild in Client.Guilds)
                {
                    total += guild.MemberCount;
                }
                return total;
            }
            catch
            {
                Logger.Log("Couldnt execute GetMemberCount()");
                return -1;
            }
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

        static async Task Main(string[] args)
        {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

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
                    Logger.Log("Parsing config");
                    Config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(Config.Filename));
                    Config.Verify();
                    Logger.Log("Parsed config", LogLevel.Success);
                    break;
                }
                catch(Exception ex)
                {
                    Logger.Log($"Error when parsing config: {ex.Message}", LogLevel.Error);
                    Logger.Log("Press enter to try again", LogLevel.Info);
                    Console.ReadLine();
                }
            }

            Logger.Log("Loading Modules", LogLevel.Info);

            Utils.Benchmark(() =>
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
            }, "Modules");

            Client.MessageReceived += Client_MessageReceived;
            Client.Ready += Client_Ready;
            Client.Disconnected += Client_Disconnected;

            await Client.LoginAsync(TokenType.Bot, Config.DISCORD_API_KEY);
            Logger.Log("Logging in...");
            await Client.StartAsync();

            Stopwatch updateWatch = new Stopwatch();
            while (true)
            {
                double deltaTime = ((double)updateWatch.ElapsedTicks / Stopwatch.Frequency);

                OnUpdate?.Invoke(deltaTime);

                updateWatch.Restart();
                Thread.Sleep(1);
            }
        }

        private static Task Client_Disconnected(Exception s)
        {
            Logger.Log($"[Bot Disconnected]\n{s.Message}", LogLevel.Error);
            return Task.Delay(0);
        }

        private static Task Client_Ready()
        {
            runTimeWatch.Start();
            Logger.Log($"[Bot Connect]\nUser={Client.CurrentUser.Username}", LogLevel.Success);
            Client.SetGameAsync(">help");
            return Task.Delay(0);
        }

        private static Task Client_MessageReceived(SocketMessage s)
        {
            if (s.Author.IsBot)
                return Task.Delay(0);

            Logger.Log($"[{(s.Channel as SocketGuildChannel).Guild.Name}]" + s.Channel.Name + "->" + s.Author.Username + ": " + s.Content);

            //This is here so i can more easily run instances of the same bot
            if (s.Content.ToLower() == ">ignore" && s.Author.Id == Config.BotOwnerID)
            {
                IgnoreMessages = true;

                s.Channel.SendMessageAsync("I will no longer handle commands");
            }

            if (s.Content.ToLower() == ">listen" && s.Author.Id == Config.BotOwnerID)
            {
                IgnoreMessages = false;

                s.Channel.SendMessageAsync("I will handle commands again");
            }

            if (s.Content.ToLower() == ">die" && s.Author.Id == Config.BotOwnerID)
            {
                s.Channel.SendMessageAsync("ok i die").GetAwaiter().GetResult();
                Client.LogoutAsync();
                return Task.Delay(0);
            }

            if (IgnoreMessages)
                return Task.Delay(0);

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

            OnMessageReceived?.Invoke(s);

            return Task.Delay(0);
        }

        public override string Name => "Core Module";

        public override int Order => 2;

        public CoreModule()
        {
            AddCMD("Enable a command", (sMsg, buffer) =>
            {
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

            AddCMD("Disable a command", (sMsg, buffer) =>
            {
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

            AddCMD("Sends the url for the source code hosted on github", (sMsg, buffer) =>
            {
                sMsg.Channel.SendMessageAsync($"**Here is the source code for the bot:** {Config.GithubURL}");
            }, ">github", ">source", ">code");

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
                desc += $"Run-Time: **{Utils.FormatTime(runTimeWatch.Elapsed, ago: false)}**\n";
                desc += $"Serving: **{Client.Guilds.Count} Guilds And {GetMemberCount()} Members**\n";
                desc += $"Commands Handled: **{TotalCommandsHandled}**\n";
                desc += $"Bancho API Calls: **{BanchoAPI.TotalAPICalls}**\n";
                desc += $"Loaded Modules: **{LoadedModules.Count}**\n";

                embed.WithDescription(desc);
                embed.WithColor(Color.Blue);
                sMsg.Channel.SendMessageAsync("", false, embed.Build());
            }, ">info");

            AddCMD("Shows this embed", (sMsg, buffer) =>
            {
                Pages commandPages = new Pages();

                EmbedBuilder builder = new EmbedBuilder();
                int commandCounter = 1;
                string description = "";

                void CompileEmbed()
                {
                    builder.WithAuthor("Available Commands", Client.CurrentUser.GetAvatarUrl());
                    builder.WithFooter($"{description.Length} chars");
                    builder.WithDescription(description);
                    commandPages.AddEmbed(builder.Build());
                    builder = new EmbedBuilder();
                    description = "";
                }

                foreach (Module module in LoadedModules)
                {
                    string tempDesc = "";
                    tempDesc += $"```fix\n{module.Name}```";
                    foreach (Command command in module.Commands)
                    {
                        tempDesc += $"`{commandCounter++}.` **{new CommandBuffer(command.Triggers, "").GetRemaining(", ")}** (*{command.Description}*)";
                        if (command.IsEnabled == false)
                            tempDesc += " -> `Has been disabled!`";
                        if (command.Cooldown > 0)
                            tempDesc += $" -> `Cooldown: {command.Cooldown}`";

                        tempDesc += "\n";

                        if (description.Length + tempDesc.Length < 2048)
                        {
                            description += tempDesc;
                            tempDesc = "";
                        }
                        else
                        {
                            CompileEmbed();
                            description += tempDesc;
                        }
                    }
                }

                if (description.Length > 0)
                    CompileEmbed();

                sMsg.Channel.SendPages(commandPages);
            }, ">help");
        }
    }
}
