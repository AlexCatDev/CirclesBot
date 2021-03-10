using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
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
        public ulong XP = 0;
        public ulong Money = 0;

        public int Level => XPToLevel(XP) - 1;
        public ulong MessagesSent = 0;
        public List<Item> Inventory = new List<Item>();
        public List<Badge> Badges = new List<Badge>();
        public string OsuUsername = "";
        public string CountryFlag = "";
        public OsuGamemode DefaultGamemode = OsuGamemode.Standard;
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

        public void GetProfile(ulong discordID, Action<DiscordProfile> modifyAction)
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
            modifyAction?.Invoke(profile);
            profile.WasModified = true;
            profileCache.TryAdd(discordID, profile);
        }

        private class TicTacToe
        {
            enum Piece
            {
                Empty,
                X,
                O
            }

            public SocketUser Player1, Player2;

            private SocketUser currentPlayer;

            public bool IsGameDone { get; set; } = false;

            private Piece[,] board = new Piece[3, 3];

            private Piece getPiece(int i)
            {
                return board[i % 3, i / 3];
            }

            private ISocketMessageChannel channel;

            private string emote(Piece piece)
            {
                switch (piece)
                {
                    case Piece.Empty:
                        return ":heavy_minus_sign:";
                    case Piece.X:
                        return ":x:";
                    case Piece.O:
                        return ":o:";
                    default:
                        return "WTF";
                }
            }

            private bool checkForWin(Piece p)
            {
                ///P - P - P
                ///P - P - P
                ///P - p - P

                //Rows
                if (board[0, 0] == p && board[1, 0] == p && board[2, 0] == p)
                    return true;

                if (board[0, 1] == p && board[1, 1] == p && board[2, 1] == p)
                    return true;

                if (board[0, 2] == p && board[1, 2] == p && board[2, 2] == p)
                    return true;

                //Columns
                if (board[0, 0] == p && board[0, 1] == p && board[0, 2] == p)
                    return true;

                if (board[1, 0] == p && board[1, 1] == p && board[1, 2] == p)
                    return true;

                if (board[2, 0] == p && board[2, 1] == p && board[2, 2] == p)
                    return true;

                //Diagonals
                if (board[0, 0] == p && board[1, 1] == p && board[2, 2] == p)
                    return true;

                if (board[0, 2] == p && board[1, 1] == p && board[2, 0] == p)
                    return true;


                return false;
            }

            private bool checkForTie()
            {
                for (int i = 0; i < board.Length; i++)
                {
                    if (getPiece(i) == Piece.Empty)
                        return false;
                }

                return true;
            }

            public (int, int) getIndex(string text)
            {
                string[] args = text.ToLower().Split(' ');

                int xIndex = -1;
                int yIndex = -1;

                if (args.Length == 2)
                {
                    switch (args[0])
                    {
                        case "top":
                            yIndex = 0;
                            break;
                        case "middle":
                            yIndex = 1;
                            break;
                        case "bottom":
                            yIndex = 2;
                            break;
                    }

                    switch (args[1])
                    {
                        case "left":
                            xIndex = 0;
                            break;
                        case "middle":
                            xIndex = 1;
                            break;
                        case "right":
                            xIndex = 2;
                            break;
                    }
                }
                else if (args.Length == 1)
                {
                    switch (args[0])
                    {
                        case "a1":
                            xIndex = 0;
                            yIndex = 0;
                            break;
                        case "a2":
                            xIndex = 1;
                            yIndex = 0;
                            break;
                        case "a3":
                            xIndex = 2;
                            yIndex = 0;
                            break;
                        case "b1":
                            xIndex = 0;
                            yIndex = 1;
                            break;
                        case "b2":
                            xIndex = 1;
                            yIndex = 1;
                            break;
                        case "b3":
                            xIndex = 2;
                            yIndex = 1;
                            break;

                        case "c1":
                            xIndex = 0;
                            yIndex = 2;
                            break;
                        case "c2":
                            xIndex = 1;
                            yIndex = 2;
                            break;
                        case "c3":
                            xIndex = 2;
                            yIndex = 2;
                            break;
                        case "middle":
                            xIndex = 1;
                            yIndex = 1;
                            break;
                    }
                }

                return (xIndex, yIndex);
            }

            private void sendGameEmbed()
            {
                EmbedBuilder builder = new EmbedBuilder();

                builder.WithAuthor($"Tic Tac Toe Game Between {Player1.Username} and {Player2.Username}", Player1.GetAvatarUrl());
                builder.WithThumbnailUrl(Player2.GetAvatarUrl());

                builder.Description += $":black_large_square: :one: :two: :three:\n";
                builder.Description += $":regional_indicator_a: {emote(getPiece(0))} {emote(getPiece(1))} {emote(getPiece(2))}\n";
                builder.Description += $":regional_indicator_b: {emote(getPiece(3))} {emote(getPiece(4))} {emote(getPiece(5))}\n";
                builder.Description += $":regional_indicator_c: {emote(getPiece(6))} {emote(getPiece(7))} {emote(getPiece(8))}\n";

                builder.WithFooter($"It's currently {currentPlayer}'s turn");

                channel.SendMessageAsync("", false, builder.Build());
            }

            public TicTacToe(SocketUser player1, SocketUser player2, ISocketMessageChannel channel)
            {
                this.channel = channel;
                Player1 = player1;
                Player2 = player2;

                if (Utils.GetRandomNumber(1, 2) == 1)
                    currentPlayer = Player1;
                else
                    currentPlayer = player2;

                sendGameEmbed();
            }

            public void ParseMessage(SocketMessage sMsg)
            {
                if (sMsg.Author.Id == currentPlayer.Id && sMsg.Channel.Id == channel.Id)
                {
                    var indices = getIndex(sMsg.Content);

                    int xIndex = indices.Item1;
                    int yIndex = indices.Item2;

                    if (xIndex == -1 || yIndex == -1)
                    {
                        sMsg.Channel.SendMessageAsync("invalid input");
                        return;
                    }

                    if (board[xIndex, yIndex] != Piece.Empty)
                    {
                        sMsg.Channel.SendMessageAsync("Theres already a piece there");
                        return;
                    }

                    if (currentPlayer == Player1)
                    {
                        board[xIndex, yIndex] = Piece.X;
                        if (checkForWin(Piece.X))
                            IsGameDone = true;
                        else
                            currentPlayer = Player2;
                    }
                    else
                    {
                        board[xIndex, yIndex] = Piece.O;
                        if (checkForWin(Piece.O))
                            IsGameDone = true;
                        else
                            currentPlayer = Player1;
                    }

                    sendGameEmbed();

                    if (IsGameDone)
                        sMsg.Channel.SendMessageAsync($"**{currentPlayer.Username}** Wins!");

                    if(checkForTie())
                    {
                        sMsg.Channel.SendMessageAsync($"**It's a tie!!!**");
                        IsGameDone = true;
                    }
                }
            }
        }

        private List<TicTacToe> ticTacToeGames = new List<TicTacToe>();

        private double profileSaveTimer = 0;

        public Task EnqueueProfileSave()
        {
            return Task.Factory.StartNew(() =>
            {
                int saveCounter = 0;
                double i = Utils.Benchmark(() =>
                {
                    foreach (var profile in profileCache)
                    {
                        if (profile.Value.WasModified)
                        {
                            profile.Value.WasModified = false;
                            File.WriteAllText($"{DiscordProfileDirectory}/{profile.Key}", JsonConvert.SerializeObject(profile.Value));
                            saveCounter++;
                        }
                    }
                });

                if(saveCounter > 0)
                    Logger.Log($"Saved {saveCounter} profiles took: {i} milliseconds", LogLevel.Info);
            });
        }

        public SocialModule()
        {
            Program.OnSimulateWorld += (s, e) =>
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

            AddCMD("Play tic tac toe against someone", async (sMsg, buffer) =>
            {
                if(sMsg.MentionedUsers.Count > 0)
                {
                    var userToDuel = sMsg.MentionedUsers.First();

                    if (ticTacToeGames.Any((a) => a.Player1.Id == userToDuel.Id || a.Player1.Id == sMsg.Author.Id || a.Player2.Id == userToDuel.Id || a.Player2.Id == sMsg.Author.Id))
                    {
                        await sMsg.Channel.SendMessageAsync("You or that person is already in a duel");
                        return;
                    }

                    var msg = await sMsg.Channel.SendMessageAsync($"{userToDuel.Mention} You have been challenged to a game of Tic Tac Toe by {sMsg.Author.Mention}\nDo you want to accept?");
                    msg.CreateReactionCollector((userID, emote, wasAdded) => {
                        if (userID == userToDuel.Id)
                        {
                            if (ticTacToeGames.Any((a) => a.Player1.Id == userToDuel.Id || a.Player1.Id == sMsg.Author.Id || a.Player2.Id == userToDuel.Id || a.Player2.Id == sMsg.Author.Id))
                            {
                                sMsg.Channel.SendMessageAsync("You or that person is already in a duel");
                                msg.DeleteReactionCollector();
                                return;
                            }

                            sMsg.Channel.SendMessageAsync("You have accepted the duel!");
                            ticTacToeGames.Add(new TicTacToe(sMsg.Author, userToDuel, sMsg.Channel));
                        }
                    
                    }, new Emoji("⚔️"));
                }

            }, ">ttt");

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
                    File.WriteAllText($"{DiscordProfileDirectory}/{userToCheck.Id}", JsonConvert.SerializeObject(new DiscordProfile()));
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
                    for (int i = 0; i < ticTacToeGames.Count; i++)
                    {
                        ticTacToeGames[i].ParseMessage(s);

                        if (ticTacToeGames[i].IsGameDone)
                        {
                            ticTacToeGames.RemoveAt(i);
                            Console.WriteLine("A game of tictactoe has been done.");
                        }
                    }

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
