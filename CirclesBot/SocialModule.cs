using Discord;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
        public int HPLevel = 10;
        public int HP = 10;
        public int AttackLevel = 1;
        public int StrengthLevel = 1;
        public int DefenceLevel = 1;
        public List<Item> EquipedItems = new List<Item>();

        public ulong XP = 0;
        public ulong Money = 0;

        public int Level => XPToLevel(XP) - 1;
        public ulong MessagesSent = 0;
        public List<Item> Inventory = new List<Item>();
        public List<Badge> Badges = new List<Badge>();
        public string OsuUsername = "";
        public bool IsLazy;
        public uint PreferredColor;

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

        public object profileLock = new object();

        public void GetProfile(ulong discordID, Action<DiscordProfile> modifyAction)
        {
            lock (profileLock)
            {
                DiscordProfile profile;

                if (File.Exists($"{DiscordProfileDirectory}/{discordID}"))
                {
                    profile = JsonConvert.DeserializeObject<DiscordProfile>(File.ReadAllText($"{DiscordProfileDirectory}/{discordID}"));
                }
                else
                {
                    profile = new DiscordProfile();
                }

                modifyAction?.Invoke(profile);

                File.WriteAllText($"{DiscordProfileDirectory}/{discordID}", JsonConvert.SerializeObject(profile));
            }
        }

        public SocialModule()
        {
            if (!Directory.Exists(DiscordProfileDirectory))
            {
                Logger.Log("No profile directory found, creating one", LogLevel.Warning);
                Directory.CreateDirectory(DiscordProfileDirectory);
            }

            AddCMD("View your inventory", (sMsg, buffer) =>
            {
                Discord.WebSocket.SocketUser userToCheck;

                if (sMsg.MentionedUsers.Count > 0)
                    userToCheck = sMsg.MentionedUsers.First();
                else
                    userToCheck = sMsg.Author;

                string description = "";
                EmbedBuilder builder = new EmbedBuilder();
                builder.WithAuthor($"Inventory for {userToCheck.Username}", userToCheck.GetAvatarUrl());

                GetProfile(userToCheck.Id, profile =>
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
            }, ">inventory", ">inv");

            AddCMD("View your profile", (sMsg, buffer) =>
            {
                Discord.WebSocket.SocketUser userToCheck;

                if (sMsg.MentionedUsers.Count > 0)
                    userToCheck = sMsg.MentionedUsers.First();
                else
                    userToCheck = sMsg.Author;

                EmbedBuilder builder = new EmbedBuilder();
                builder.WithAuthor($"Profile for {userToCheck.Username}", userToCheck.GetAvatarUrl());

                GetProfile(userToCheck.Id, (profile) =>
                {
                    foreach (var field in profile.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic))
                    {
                        try
                        {
                            //Console.WriteLine($"{field.Name}");
                            builder.Description += $"`{field.Name}:` **{field.GetValue(profile)?.ToString() ?? "null"}**\n";
                        }
                        catch (Exception e) { /*Console.WriteLine(e.Message);*/ }
                    }

                    foreach (var property in profile.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic))
                    {
                        try
                        {
                            //Console.WriteLine($"{field.Name}");
                            builder.Description += $"`{property.Name}:` **{property.GetValue(profile)?.ToString() ?? "null"}**\n";
                        }
                        catch (Exception e) { /*Console.WriteLine(e.Message);*/ }
                    }
                });

                if (userToCheck.Id == Program.Config.BotOwnerID)
                    builder.WithFooter($"Bot Owner");

                sMsg.Channel.SendMessageAsync("", false, builder.Build());
            }, ">profile", ">pf");

            AddCMD("itemtest", (sMsg, buffer) =>
            {
                if (sMsg.Author.Id == Program.Config.BotOwnerID)
                {
                    GetProfile(sMsg.Author.Id, (profile) =>
                    {
                        profile.Inventory.Add(new Item()
                        {
                            Name = "Test Item",
                            Damage = 1337,
                            Accuracy = 69,
                            HealAmount = 727,
                            Description = "A Test Item",
                            Icon = ":sunglasses:"
                        });
                    });

                    sMsg.Channel.SendMessageAsync($"Gave u testitem");
                }
                else
                {
                    sMsg.Channel.SendMessageAsync("no");
                }
            }, ">itemtest");

            AddCMD("Wipes a profile", (sMsg, buffer) =>
            {
                Discord.WebSocket.SocketUser userToCheck;

                if (sMsg.MentionedUsers.Count > 0)
                    userToCheck = sMsg.MentionedUsers.First();
                else
                {
                    sMsg.Channel.SendMessageAsync("Mention someone to wipe their profile");
                    return;
                }

                if (sMsg.Author.Id == Program.Config.BotOwnerID)
                {
                    lock (profileLock)
                    {
                        File.WriteAllText($"{DiscordProfileDirectory}/{userToCheck.Id}", JsonConvert.SerializeObject(new DiscordProfile()));
                    }
                    sMsg.Channel.SendMessageAsync($"Wiped **{userToCheck.Username}** :ok_hand:");
                }
                else
                {
                    sMsg.Channel.SendMessageAsync("no");
                }
            }, ">wipe");

            AddCMD("Set your xp", (sMsg, buffer) =>
            {
                ulong? xp = buffer.GetULong();

                Discord.WebSocket.SocketUser userToCheck;

                if (sMsg.MentionedUsers.Count > 0)
                    userToCheck = sMsg.MentionedUsers.First();
                else
                {
                    sMsg.Channel.SendMessageAsync("Mention someone to set their xp");
                    return;
                }

                if (sMsg.Author.Id == Program.Config.BotOwnerID)
                {
                    if(xp.HasValue == false)
                    {
                        sMsg.Channel.SendMessageAsync("you need to define how much xp");
                        return;
                    }

                    GetProfile(userToCheck.Id, profile => {
                        profile.XP = xp.Value;
                    });

                    sMsg.Channel.SendMessageAsync($"**{userToCheck.Username}'s** xp has been set to **{xp.Value}**");
                }
                else
                {
                    sMsg.Channel.SendMessageAsync("no");
                }
            }, ">xp");

            ulong lastAuthorID = 0;

            Program.Client.MessageReceived += (s) =>
            {
                if (!s.Author.IsBot)
                {
                    GetProfile(s.Author.Id, (profile) =>
                    {
                        profile.MessagesSent++;
                        //50 to 200 xp per "unique" message
                        if (lastAuthorID != s.Author.Id)
                        {
                            lastAuthorID = s.Author.Id;
                            int lvl = profile.Level;
                            profile.XP += (ulong)Utils.GetRandomNumber(50, 200);
                            if(lvl == 98 && profile.Level == 99)
                            {
                                profile.Inventory.Add(ItemCreator.Create99Skillcape());
                                s.Channel.SendMessageAsync($"{s.Author.Mention}\n**Congratz on level 99!!! Now get a fucking life**\n{Program.XD}");
                            }
                        }
                    });
                }

                return Task.Delay(0);
            };
        }
    }
}
