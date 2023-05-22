using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CirclesBot
{
    public static class ItemCreator
    {
        public static Item Create99Skillcape()
        {
            Item i = new Item();
            i.Icon = "<:99cape:813835496748482570>";
            i.Name = "Level 99 Skillcape";
            i.Description = "Gzz on level 99!";
            return i;
        }

        public static Item CreateDragonClaws()
        {
            Item i = new Item();
            i.Icon = "<:DragonClaws:813843093156528139>";
            i.Name = "Dragon claws";
            i.Description = "A set of fighting claws.";
            i.Accuracy = 57;
            i.Damage = 56;
            return i;
        }

        public static Item CreateArmadylGodsword()
        {
            Item i = new Item();
            i.Icon = "<:ArmadylGodsword:813843630464434256>";
            i.Name = "Armadyl Godsword";
            i.Description = "A beautiful, heavy sword.";
            i.Accuracy = 132;
            i.Damage = 132;
            return i;
        }

        public static Item CreateShark()
        {
            Item i = new Item();
            i.Icon = "<:Shark:813844041044328469>";
            i.Name = "Shark";
            i.Description = "I'd better be careful eating this.";
            i.HealAmount = 20;
            return i;
        }
    }

    public class Item
    {
        public string Icon;
        public string Name;
        public string Description;

        public int Damage = -1;
        public int Accuracy = -1;

        public int HealAmount = -1;
    }

    public class Badge
    {
        public string Name;
        public string Icon;
    }

    public class DiscordProfile
    {
        public ulong XP = 0;
        public ulong Money = 0;

        public int Level => XPToLevel(XP) - 1;
        public ulong MessagesSent = 0;
        public List<Item> Inventory = new List<Item>();
        public List<Badge> Badges = new List<Badge>();
        public List<int> Codes = new List<int>();
        public DateTime LastCommand;
        public string OsuUsername = "";
        public string CountryFlag = "";
        public OsuGamemode DefaultGamemode = OsuGamemode.VANILLA_OSU;
        public bool IsLazy;
        public uint PreferredColor;
        public bool WasModified;

        /// <summary>
        /// Runescape formula :P
        /// </summary>
        /// <param name="xp"></param>
        /// <returns></returns>
        private int XPToLevel(ulong xp)
        {
            int level = 1;

            while (experienceForLevel(level) <= xp) {
                level++;
            }

            return level;
        }

        private ulong experienceForLevel(int level)
        {
            double total = 0;

            for (int i = 1; i < level; i++)
            {
                total += Math.Floor(i + (300 * Math.Pow(2, i / 7.0)));
            }

            return (ulong)Math.Floor(total / 4.0);
        }
    }

    public class SocialModule : Module
    {
        public override string Name => "Social Module";

        public override int Order => 1;

        public const string DiscordProfileDirectory = "./Profiles";

        private ConcurrentDictionary<ulong, DiscordProfile> profileCache = new ConcurrentDictionary<ulong, DiscordProfile>();

        //Key channel ID. Value: Author id, for mapping author to channel
        private Dictionary<ulong, ulong> channelToLastAuthor = new Dictionary<ulong, ulong>();

        public void ModifyProfile(ulong discordID, Action<DiscordProfile> modifyAction)
        {
            DiscordProfile profile = null;

            //if not in cache
            if (profileCache.TryGetValue(discordID, out profile) == false)
            {
                //if not in cache, attempt to read from disk
                if (File.Exists($"{DiscordProfileDirectory}/{discordID}"))
                {
                    profile = JsonConvert.DeserializeObject<DiscordProfile>(File.ReadAllText($"{DiscordProfileDirectory}/{discordID}"));
                }
            }

            if(profile == null)
                profile = new DiscordProfile();

            modifyAction?.Invoke(profile);
            profile.WasModified = true;
            profileCache.TryAdd(discordID, profile);
        }

        public DiscordProfile GetProfile(ulong discordID)
        {
            DiscordProfile profile = null;

            //if not in cache
            if (profileCache.TryGetValue(discordID, out profile) == false)
            {
                //if not in cache, attempt to read from disk
                if (File.Exists($"{DiscordProfileDirectory}/{discordID}"))
                {
                    profile = JsonConvert.DeserializeObject<DiscordProfile>(File.ReadAllText($"{DiscordProfileDirectory}/{discordID}"));
                }
                else
                {
                    //if not on disk create new
                    profile = new DiscordProfile();
                }
            }

            profileCache.TryAdd(discordID, profile);

            return profile;
        }

        private double profileSaveTimer = 0;

        public Task EnqueueProfileSave()
        {
            return Task.Factory.StartNew(() =>
            {
                int saveCounter = 0;
                Stopwatch sw = Stopwatch.StartNew();

                foreach (var profile in profileCache)
                {
                    if (profile.Value == null)
                        continue;

                    if (profile.Value.WasModified)
                    {
                        profile.Value.WasModified = false;

                        profile.Value.Save($"{DiscordProfileDirectory}/{profile.Key}");
                        saveCounter++;
                    }
                }

                sw.Stop();

                if (saveCounter > 0)
                    Logger.Log($"Saved {saveCounter} profiles in {sw.ElapsedMilliseconds} MS", LogLevel.Info);
            });
        }

        public SocialModule()
        {
            CoreModule.OnUpdate += (e) =>
            {
                profileSaveTimer += e;
                if (profileSaveTimer >= 10.0)
                {
                    EnqueueProfileSave();
                    profileSaveTimer = 0;
                }
            };

            if (!Directory.Exists(DiscordProfileDirectory))
            {
                Logger.Log("No profile directory found, creating one", LogLevel.Warning);
                Directory.CreateDirectory(DiscordProfileDirectory);
            }

            AddCMD("View your inventory", (sMsg, buffer) =>
            {
                SocketUser userToCheck;

                if (sMsg.MentionedUsers.Count > 0)
                    userToCheck = sMsg.MentionedUsers.First();
                else
                    userToCheck = sMsg.Author;

                string description = "";
                EmbedBuilder builder = new EmbedBuilder();
                builder.WithAuthor($"Inventory for {userToCheck.Username}", userToCheck.GetAvatarUrl());

                ModifyProfile(userToCheck.Id, profile =>
                {
                    foreach (var item in profile.Inventory)
                    {
                        description += $"▸ {item.Icon} ▸ {item.Name}\n";
                    }

                    builder.WithDescription(description);
                    builder.WithColor(new Color(profile.PreferredColor));
                    builder.WithFooter($"{profile.Inventory.Count} Items");
                });

                sMsg.Channel.SendMessageAsync("", false, builder.Build());
            }, ".inventory", ".inv");

            AddCMD("View your profile", (sMsg, buffer) =>
            {
                SocketUser userToCheck;

                if (sMsg.MentionedUsers.Count > 0)
                    userToCheck = sMsg.MentionedUsers.First();
                else
                    userToCheck = sMsg.Author;

                EmbedBuilder builder = new EmbedBuilder();
                builder.WithAuthor($"Profile for {userToCheck.Username}", userToCheck.GetAvatarUrl());

                ModifyProfile(userToCheck.Id, (profile) =>
                {
                    foreach (var field in profile.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic))
                    {
                        try
                        {
                            string value = field.GetValue(profile)?.ToString();

                            if (string.IsNullOrEmpty(value))
                                value = "null";

                            builder.Description += $"`{field.Name}:` **{value}**\n";
                        }
                        catch (Exception e) { /*Console.WriteLine(e.Message);*/ }
                    }

                    foreach (var property in profile.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic))
                    {
                        try
                        {
                            //Console.WriteLine($"{field.Name}");
                            string value = property.GetValue(profile)?.ToString();

                            if (string.IsNullOrEmpty(value))
                                value = "null";

                            builder.Description += $"`{property.Name}:` **{value}**\n";
                        }
                        catch (Exception e) { /*Console.WriteLine(e.Message);*/ }
                    }
                });

                if (userToCheck.Id == CoreModule.Config.BotOwnerID)
                    builder.WithFooter($"Bot Owner");

                sMsg.Channel.SendMessageAsync("", false, builder.Build());
            }, ".profile", ".pf");

            AddCMD("Wipes a profile", (sMsg, buffer) =>
            {
                SocketUser userToCheck;

                if (sMsg.MentionedUsers.Count > 0)
                    userToCheck = sMsg.MentionedUsers.First();
                else
                {
                    sMsg.Channel.SendMessageAsync("Mention someone to wipe their profile");
                    return;
                }

                if (sMsg.Author.Id == CoreModule.Config.BotOwnerID)
                {
                    File.WriteAllText($"{DiscordProfileDirectory}/{userToCheck.Id}", JsonConvert.SerializeObject(new DiscordProfile()));
                    sMsg.Channel.SendMessageAsync($"Wiped **{userToCheck.Username}** :ok_hand:");
                }
                else
                {
                    sMsg.Channel.SendMessageAsync("no");
                }
            }, ".wipe");

            AddCMD("Save code ;)", (sMsg, buffer) =>
            {
                var code = buffer.GetInt();
                if (code.HasValue)
                {
                    ModifyProfile(sMsg.Author.Id, (profile) => {
                        if (profile.Codes.Contains(code.Value))
                        {
                            sMsg.Channel.SendMessageAsync("you already have this");
                        }
                        else
                        {
                            profile.Codes.Add(code.Value);
                            sMsg.Channel.SendMessageAsync("**;)**");
                        }
                    });
                }
                else
                {
                    sMsg.Channel.SendMessageAsync("Not a valid code");
                }
            }, ".s");

            AddCMD("Set your xp", (sMsg, buffer) =>
            {
                ulong? xp = buffer.GetULong();

                SocketUser userToCheck;

                if (sMsg.MentionedUsers.Count > 0)
                    userToCheck = sMsg.MentionedUsers.First();
                else
                {
                    sMsg.Channel.SendMessageAsync("Mention someone to set their xp");
                    return;
                }

                if (sMsg.Author.Id == CoreModule.Config.BotOwnerID)
                {
                    if(xp.HasValue == false)
                    {
                        sMsg.Channel.SendMessageAsync("you need to define how much xp");
                        return;
                    }

                    ModifyProfile(userToCheck.Id, profile => {
                        profile.XP = xp.Value;
                    });

                    sMsg.Channel.SendMessageAsync($"**{userToCheck.Username}'s** xp has been set to **{xp.Value}**");
                }
                else
                {
                    sMsg.Channel.SendMessageAsync("no");
                }
            }, ".xp");

            AddCMD("Hit the bell as hard as you can", (sMsg, buffer) =>
            {
                Dictionary<int, string[]> responses = new Dictionary<int, string[]>() { 
                    { 1, new[] { "Dårlig", "nej", "svans", "per" } },
                    { 2, new[] { "Less bad", "Less no" } },
                    { 3, new[] { "Very not good", "Lol", "zzzzz" } },
                    { 4, new[] { "Too bad", "Never lucky", "Jeg ved ikk hvad jeg sidder og laver lige nu hils hvis du ser det her" } },
                    { 5, new[] { "Sex", "50/50" } },
                    { 6, new[] { "Almost good", "Cum", "Klokken er mange og jeg er træt" } },
                    { 7, new[] { "Genetics capped", "Fuck jeg har ondt i maven" } },
                    { 8, new[] { "blue zenith", "Wtf" } },
                    { 9, new[] { "Cookiezi 727", "KekW" } },
                    { 10, new[] { "Good use of your time :thumb_up:", "Get a life", "Flot klaret" } },
                };

                EmbedBuilder embedBuilder = new EmbedBuilder();

                embedBuilder.WithAuthor("Ram klokken så hårdt du kan", sMsg.Author.GetAvatarUrl());
                embedBuilder.Description += $"🔔\n";
                embedBuilder.Description += $"░\n";
                embedBuilder.Description += $"░\n";
                embedBuilder.Description += $"░\n";
                embedBuilder.Description += $"░\n";
                embedBuilder.Description += $"░\n";
                embedBuilder.Description += $"░\n";
                embedBuilder.Description += $"░\n";
                embedBuilder.Description += $"░\n";
                embedBuilder.Description += $"░\n";
                embedBuilder.Description += $"░\n";

                var sendMsg = sMsg.Channel.SendMessageAsync(embed: embedBuilder.Build()).Result;

                Extensions.CreateReactionCollector(sendMsg, (id, emote, wasAdded) => { 
                    if(id == sMsg.Author.Id)
                    {
                        embedBuilder.Description = "🔔\n";

                        int num = Utils.GetRandomNumber(1, 100);

                        int ticks = (int)Math.Floor((num * 10d) / 100d);

                        for (int i = 0; i < 10 - ticks; i++)
                        {
                            embedBuilder.Description += $"░\n";
                        }

                        var response = responses[ticks];

                        for (int i = 0; i < ticks; i++)
                        {
                            embedBuilder.Description += $"█{(i == 0 ? $"{num}/100 {response[Utils.GetRandomNumber(0, response.Length - 1)]}" : "")}\n";
                        }

                        sendMsg.ModifyAsync((o) => { o.Embed = embedBuilder.Build(); });

                        sendMsg.DeleteReactionCollector();
                    }
                }, new Emoji("🔔"));
            }, ".bell");

            CoreModule.OnMessageReceived += (s) =>
            {
                ModifyProfile(s.Author.Id, (profile) =>
                {
                    profile.MessagesSent++;

                    if (channelToLastAuthor.TryGetValue(s.Channel.Id, out ulong authorID))
                    {
                        //Only grant xp if another user has written since this message
                        if(authorID != s.Author.Id)
                        {
                            //update author id to this users id
                            channelToLastAuthor[s.Channel.Id] = s.Author.Id;

                            int lvl = profile.Level;
                            profile.XP += (ulong)Utils.GetRandomNumber(50, 200);
                            if (lvl == 98 && profile.Level == 99)
                            {
                                profile.Inventory.Add(ItemCreator.Create99Skillcape());
                                s.Channel.SendMessageAsync($"{s.Author.Mention}\n**Congratz on level 99!!! Now get a fucking life**\n{CoreModule.XD}");
                            }
                        }
                    }
                    else
                    {
                        //If no key, then it's first message in this channel since bot started, idk just add and dont grant xp
                        channelToLastAuthor.Add(s.Channel.Id, s.Author.Id);
                    }
                });
            };
        }
    }
}
