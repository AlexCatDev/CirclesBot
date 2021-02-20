﻿using Discord;
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
    public class Item
    {
        public string Icon;
        public string Name;
        public string Description;

        public int Damage;
        public int Accuracy;

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
        public int Level => XPToLevel(XP) - 1;
        public ulong MessagesSent = 0;
        public List<Item> Inventory = new List<Item>();
        public List<Badge> Badges = new List<Badge>();
        public string OsuUsername = "";
        public bool IsLazy;
        public int PreferredColor;


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
                total += Math.Floor(i + 300 * Math.Pow(2, i / 7.0));
            }

            return (ulong)Math.Floor(total / 4.0);
        }
    }

    public class SocialModule : Module
    {
        public override string Name => "Social Module";

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
                string output = "";
                GetProfile(sMsg.Author.Id, profile =>
                {
                    foreach (var item in profile.Inventory)
                    {
                        output += $"{item.Icon} : {item.Name} [{item.Description}] -> {item.Damage}\n";
                    }
                });
                if (output == "")
                    sMsg.Channel.SendMessageAsync("You have no items sir");
                else
                    sMsg.Channel.SendMessageAsync(output);
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
                            profile.XP += (ulong)Utils.GetRandomNumber(50, 200);
                        }
                    });
                }

                return Task.Delay(0);
            };
        }
    }
}
