using Discord;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace CirclesBot
{
    /// <summary>
    /// GIANT TODO LIST
    /// 
    /// 1. Make it so every score posting command adds a list rather than a single score, so it's easier for comparisons
    /// 2. Instead of downloading every beatmap and doing pp calculations with EZPP, use /get_beatmap endpoint for getting difficulty values.
    /// 3. Use osu!lazer pp calculator for ^
    /// 4. Remove EZPP after ^
    /// 5. Cleanup code
    /// 6. Add Ripple/Akatsuki/Gatari support lowpriority
    /// 7. Add Mania/CTB/Taiko support midpriority
    /// </summary>

    public class OsuModule : Module
    {
        public override string Name => "osu! Module";

        //(ulong: Discord user id), (string: osu! username)
        private Dictionary<ulong, string> discordUserToOsuUser = new Dictionary<ulong, string>();

        //(ulong: Discord channel id), (ulong: osu! beatmap id)
        private Dictionary<ulong, ulong> discordChannelToBeatmap = new Dictionary<ulong, ulong>();

        //(ulong: Discord channel id), (self explainatory, fix to OsuScore soon?)
        private Dictionary<ulong, List<BanchoAPI.BanchoBestScore>> discordChannelToBeatmapTop = new Dictionary<ulong, List<BanchoAPI.BanchoBestScore>>();

        private BanchoAPI banchoAPI;

        private EmbedBuilder CreateProfileEmbed(OsuProfile osuProfile)
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();
            embedBuilder.Description += $"▸ **Official Rank:** #{osuProfile.Rank} ({osuProfile.Country}#{osuProfile.CountryRank})\n";
            embedBuilder.Description += $"▸ **Level:** {osuProfile.Level.ToString("F2")}\n▸ **Total PP:** {osuProfile.PP.ToString("F2")}\n";
            embedBuilder.Description += $"▸ **Hit Accuracy:** {osuProfile.Accuracy.ToString("F2")}%\n▸ **Playcount:** {osuProfile.Playcount}";

            embedBuilder.WithColor(new Color(Utils.GetRandomNumber(0, 255), Utils.GetRandomNumber(0, 255), Utils.GetRandomNumber(0, 255)));
            return embedBuilder;
        }

        private EmbedBuilder CreateScoresEmbed(List<OsuScore> scores)
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();

            int count = 0;
            string description = "";

            foreach (var score in scores)
            {
                count++;

                string temp = "";

                string isFCInfo = "";
 
                if(!score.IsFC)
                    isFCInfo = $" ({score.PP_IF_FC.ToString("F2")}PP for {score.IF_FC_Accuracy.ToString("F2")}% FC)";

                temp += $"**{count}.** [**{score.SongName} [{score.DifficultyName}]**]({BanchoAPI.GetBeatmapUrl(score.BeatmapID.ToString())}) **+{score.EnabledMods.ToFriendlyString()}** [{score.StarRating.ToString("F2")}★]";

                if (Utils.GetRandomChance(1))
                    temp += ":tada:";

                temp += "\n";

                temp += $"▸ {Utils.GetEmoteForRankLetter(score.RankingLetter)} ▸ **{score.PP.ToString("F2")}PP**{isFCInfo} ▸ {score.Accuracy.ToString("F2")}%\n";
                temp += $"▸ {score.Score} ▸ x{score.MaxCombo}/{score.MapMaxCombo} ▸ [{score.Count300}/{score.Count100}/{score.Count50}/{score.CountMiss}]\n";
                temp += $"▸ **AR:** {score.AR.ToString("F1")} **OD:** {score.OD.ToString("F1")} **HP:** {score.HP.ToString("F1")} **CS:** {score.CS.ToString("F1")} ▸ **BPM:** {score.BPM.ToString("F0")}\n";

                if (!score.IsPass)
                    temp += $"▸ **Map Completion:** {score.CompletionPercentage.ToString("F2")}%\n";

                temp += $"▸ Score set {Utils.FormatTime(DateTime.UtcNow - score.Date)} On {score.Server}\n";

                if (temp.Length + description.Length < 2048)
                {
                    description += temp;
                }
                else
                {
                    count--;
                    break;
                }
            }

            

            embedBuilder.WithDescription(description);

            embedBuilder.WithFooter($"Plays shown: {count}/{scores.Count}");

            embedBuilder.WithColor(new Color(Utils.GetRandomNumber(0, 255), Utils.GetRandomNumber(0, 255), Utils.GetRandomNumber(0, 255)));



            return embedBuilder;
        }

        public OsuModule()
        {
            
            int time = Utils.Benchmark(() => {
                BeatmapManager.LoadAllMaps();
            });
            Logger.Log($"Loaded {BeatmapManager.CachedMapCount} local beatmaps it took {time} milliseconds", LogLevel.Success);
            
            banchoAPI = new BanchoAPI(Credentials.OSU_API_KEY);

            if (File.Exists("./OsuUsers"))
            {
                discordUserToOsuUser = JsonConvert.DeserializeObject<Dictionary<ulong, string>>(File.ReadAllText("./OsuUsers"));

                Logger.Log($"Added: {discordUserToOsuUser.Count} osu! users to the dictionary", LogLevel.Info);
            }
            else
            {
                Logger.Log($"Couldn't find a OsuUsers file!", LogLevel.Warning);
            }

            //Optimized
            Commands.Add(new Command("Shows recent plays for user", (sMsg, buffer) =>
            {
                Stopwatch sw = Stopwatch.StartNew();

                string userToCheck = "";
                
                if (sMsg.MentionedUsers.Count > 0)
                {
                    if(!discordUserToOsuUser.TryGetValue(sMsg.MentionedUsers.First().Id, out userToCheck))
                    {
                        sMsg.Channel.SendMessageAsync("That person has not linked their osu! account.");
                        return;
                    }
                }

                bool showList = buffer.HasParameter("-l");

                if (userToCheck == "")
                    userToCheck = buffer.GetRemaining();

                if (userToCheck == "")
                {
                    if (!discordUserToOsuUser.TryGetValue(sMsg.Author.Id, out userToCheck))
                    {
                        sMsg.Channel.SendMessageAsync("Please mention someone, use their username or set your own with **>osuset <username>**");
                        return;
                    }
                }

                Logger.Log($"getting recent plays for '{userToCheck}'", LogLevel.Info);

            try
            {
                List<BanchoAPI.BanchoRecentScore> recentUserPlays = banchoAPI.GetRecentPlays(userToCheck, showList ? 20 : 1);

                if (recentUserPlays.Count == 0)
                {
                    sMsg.Channel.SendMessageAsync($"**{userToCheck} don't have any recent plays** <:sadChamp:593405356864962560>");
                    return;
                }

                if (discordChannelToBeatmap.ContainsKey(sMsg.Channel.Id))
                    discordChannelToBeatmap[sMsg.Channel.Id] = recentUserPlays[0].BeatmapID;
                else
                    discordChannelToBeatmap.Add(sMsg.Channel.Id, recentUserPlays[0].BeatmapID);

                List<OsuScore> kek = new List<OsuScore>();

                foreach (var rup in recentUserPlays)
                {
                    kek.Add(new OsuScore(BeatmapManager.GetBeatmap(rup.BeatmapID), rup));
                }

                EmbedBuilder embedBuilder = CreateScoresEmbed(kek);

                embedBuilder.WithThumbnailUrl(BanchoAPI.GetBeatmapImageUrl(Utils.FindBeatmapSetID(BeatmapManager.GetBeatmap(kek[0].BeatmapID)).ToString()));

                    embedBuilder.WithAuthor($"Recent Plays for {userToCheck}", BanchoAPI.GetProfileImageUrl(recentUserPlays[0].UserID.ToString()));

                    sMsg.Channel.SendMessageAsync("", false, embedBuilder.Build());
                }
                catch (Exception ex)
                {
                    Logger.Log(ex.StackTrace, LogLevel.Error);
                    sMsg.Channel.SendMessageAsync("uh oh something happend check console");
                }
            }, ">rs", ">recent"));


            Commands.Add(new Command("Shows user plays on a specific map", (sMsg, buffer) =>
            {
                Stopwatch sw = Stopwatch.StartNew();

                string userToCheck = "";

                if (sMsg.MentionedUsers.Count > 0)
                {
                    if (!discordUserToOsuUser.TryGetValue(sMsg.MentionedUsers.First().Id, out userToCheck))
                    {
                        sMsg.Channel.SendMessageAsync("That person has not linked their osu! account.");
                        return;
                    }
                }

                bool ripple = buffer.HasParameter("-ripple");
                string beatmap = buffer.GetParameter("https://osu.ppy.sh/beatmapsets/");
                ulong beatmapSetID = 0;
                ulong beatmapID = 0;

                try
                {
                    beatmapSetID = ulong.Parse(beatmap.Split("#osu/")[0]);
                    beatmapID = ulong.Parse(beatmap.Split("#osu/")[1]);
                }
                catch
                {
                    sMsg.Channel.SendMessageAsync("Error parsing beatmap url.");
                    return;
                }

                if (userToCheck == "")
                    userToCheck = buffer.GetRemaining();

                if (userToCheck == "")
                {
                    if (!discordUserToOsuUser.TryGetValue(sMsg.Author.Id, out userToCheck))
                    {
                        sMsg.Channel.SendMessageAsync("Please mention someone, use their username or set your own with **>osuset <username>**");
                        return;
                    }
                }

                Logger.Log($"getting recent plays for '{userToCheck}'", LogLevel.Info);

                try
                {
                    List<BanchoAPI.BanchoScore> userPlays = banchoAPI.GetScores(userToCheck, beatmapID, 10);

                    if (userPlays.Count == 0)
                    {
                        sMsg.Channel.SendMessageAsync($"**{userToCheck} don't have any plays on this map** <:sadChamp:593405356864962560>");
                        return;
                    }

                    if (discordChannelToBeatmap.ContainsKey(sMsg.Channel.Id))
                        discordChannelToBeatmap[sMsg.Channel.Id] = beatmapID;
                    else
                        discordChannelToBeatmap.Add(sMsg.Channel.Id, beatmapID);

                    List<OsuScore> kek = new List<OsuScore>();

                    foreach (var rup in userPlays)
                    {
                        kek.Add(new OsuScore(BeatmapManager.GetBeatmap(beatmapID), rup, beatmapID));
                    }

                    EmbedBuilder embedBuilder = CreateScoresEmbed(kek);

                    embedBuilder.WithThumbnailUrl(BanchoAPI.GetBeatmapImageUrl(Utils.FindBeatmapSetID(BeatmapManager.GetBeatmap(beatmapID)).ToString()));

                    embedBuilder.WithAuthor($"Plays for {userToCheck}", BanchoAPI.GetProfileImageUrl(userPlays[0].UserID.ToString()));

                    sMsg.Channel.SendMessageAsync("", false, embedBuilder.Build());
                }
                catch (Exception ex)
                {
                    Logger.Log(ex.StackTrace, LogLevel.Error);
                    sMsg.Channel.SendMessageAsync("uh oh something happend check console");
                }
            }, ">scores", ">sc"));

            Commands.Add(new Command("Shows top plays for user", (sMsg, buffer) =>
            {
                Stopwatch sw = Stopwatch.StartNew();

                string userToCheck = "";

                if (sMsg.MentionedUsers.Count > 0)
                {
                    if (!discordUserToOsuUser.TryGetValue(sMsg.MentionedUsers.First().Id, out userToCheck))
                    {
                        sMsg.Channel.SendMessageAsync("That person has not linked their osu! account.");
                        return;
                    }
                }

                bool showRecent = buffer.HasParameter("-r");

                bool isRipple = buffer.HasParameter("-ripple");

                if (userToCheck == "")
                    userToCheck = buffer.GetRemaining();

                if (userToCheck == "")
                {
                    if (!discordUserToOsuUser.TryGetValue(sMsg.Author.Id, out userToCheck))
                    {
                        sMsg.Channel.SendMessageAsync("Please mention someone, use their username or set your own with **>osuset <username>**");
                        return;
                    }
                }

                EmbedBuilder embedBuilder = new EmbedBuilder();

                Logger.Log($"Getting best plays for '{userToCheck}'", LogLevel.Info);

                try
                {
                    List<BanchoAPI.BanchoBestScore> bestUserPlays = banchoAPI.GetBestPlays(userToCheck, 100);
                    
                    if (bestUserPlays.Count == 0)
                    {
                        sMsg.Channel.SendMessageAsync($"**{userToCheck} doesn't have any top plays** :face_with_raised_eyebrow:");
                        return;
                    }

                    if (showRecent)
                        bestUserPlays.Sort((x, y) => DateTime.Compare(y.DateOfPlay, x.DateOfPlay));
                    else
                        bestUserPlays.Sort((x, y) => y.PP.CompareTo(x.PP));

                    try
                    {
                        int index = Math.Min(10, bestUserPlays.Count);
                        bestUserPlays.RemoveRange(index, bestUserPlays.Count - index);
                    }
                    catch { }

                    discordChannelToBeatmapTop.LazyAdd(sMsg.Channel.Id, bestUserPlays);


                    List<OsuScore> kek = new List<OsuScore>();
                    for (int i = 0; i < bestUserPlays.Count; i++)
                    {
                        var play = bestUserPlays[i];
                        kek.Add(new OsuScore(BeatmapManager.GetBeatmap(play.BeatmapID), play));
                    }

                    embedBuilder = CreateScoresEmbed(kek);
                    string recent = showRecent ? "Recent " : "";
                    embedBuilder.WithAuthor($"Top {recent}osu! Plays for {userToCheck}", BanchoAPI.GetProfileImageUrl(kek[0].UserID.ToString()));
                    embedBuilder.WithThumbnailUrl(BanchoAPI.GetBeatmapImageUrl(Utils.FindBeatmapSetID(BeatmapManager.GetBeatmap(kek[0].BeatmapID)).ToString()));
                    sMsg.Channel.SendMessageAsync("", false, embedBuilder.Build());
                }
                catch (Exception ex)
                {
                    Logger.Log(ex.StackTrace, LogLevel.Error);
                    sMsg.Channel.SendMessageAsync("uh oh something happend check console");
                }
            }, ">top", ">osutop"));

            Commands.Add(new Command("Get PP For fc", (sMsg, buffer) =>
            {
                buffer.Discard("%");

                int? indexToCheck = buffer.GetInt();

                double? accuracy = buffer.GetDouble();

                if (accuracy == null)
                    accuracy = indexToCheck;

                string beatmap = buffer.GetParameter("https://osu.ppy.sh/beatmapsets/");
                ulong beatmapSetID = 0;
                ulong beatmapID = 0;

                if (beatmap != "")
                {
                    try
                    {
                        beatmapSetID = ulong.Parse(beatmap.Split("#osu/")[0]);
                        beatmapID = ulong.Parse(beatmap.Split("#osu/")[1]);
                    }
                    catch
                    {
                        sMsg.Channel.SendMessageAsync("Error parsing beatmap url.");
                        return;
                    }
                }
                else
                {

                    //string mds = buffer.GetRemaining().Split(;

                    if (indexToCheck == null)
                    {
                        if (!discordChannelToBeatmap.TryGetValue(sMsg.Channel.Id, out beatmapID))
                        {
                            sMsg.Channel.SendMessageAsync("No beatmap found in conversation");
                            return;
                        }
                    }
                    else
                    {
                        if (!discordChannelToBeatmapTop.TryGetValue(sMsg.Channel.Id, out List<BanchoAPI.BanchoBestScore> bestPlays))
                        {
                            sMsg.Channel.SendMessageAsync("No beatmap top found in conversation");
                            return;
                        }
                        else
                        {
                            indexToCheck = Math.Max(indexToCheck.Value - 1, 0);
                            beatmapID = bestPlays[Math.Min(bestPlays.Count, indexToCheck.Value)].BeatmapID;

                            if (discordChannelToBeatmap.ContainsKey(sMsg.Channel.Id))
                                discordChannelToBeatmap[sMsg.Channel.Id] = beatmapID;
                            else
                                discordChannelToBeatmap.Add(sMsg.Channel.Id, beatmapID);
                        }
                    }
                }

                bool hasMods = Enum.TryParse<Mods>(buffer.GetRemaining(), true, out Mods mods);

                Logger.Log($"calculating if pp fc for {beatmapID}", LogLevel.Info);

                try
                {
                    if(accuracy == null)
                    {
                        sMsg.Channel.SendMessageAsync("Example: >pp 98.5% HD,DT");
                        return;
                    }

                    if (!hasMods)
                        mods = Mods.None;

                    var ez = EZPP.Calculate(BeatmapManager.GetBeatmap(beatmapID), 0, 0, 0, 0, Mods.None);

                    double estimatedCount100 = (accuracy.Value / 100.0) / (double)ez.TotalHitObjects;


                    ez = EZPP.Calculate(BeatmapManager.GetBeatmap(beatmapID), ez.MaxCombo, (int)Math.Floor(estimatedCount100), 0, 0, mods);
                    sMsg.Channel.SendMessageAsync($"`FCPP` for **{accuracy}%** and mods **{mods.ToFriendlyString()}** is: **{ez.PP.ToString("F2")}** on **{ez.SongName} [{ez.DifficultyName}]**");
                }
                catch (Exception ex)
                {
                    Logger.Log(ex.Message, LogLevel.Warning);
                    Logger.Log(ex.StackTrace, LogLevel.Error);
                    sMsg.Channel.SendMessageAsync("uh oh something happend check console");
                }
            }, ">pp", ">fc"));

            Commands.Add(new Command("Compares plays for user", (sMsg, buffer) =>
            {
                string userToCheck = "";

                bool isRipple = buffer.HasParameter("-ripple");

                int? indexToCheck = buffer.GetInt();

                ulong beatmapID = 0;

                if (userToCheck == "")
                    userToCheck = buffer.GetRemaining();

                if (sMsg.MentionedUsers.Count > 0)
                {
                    if (!discordUserToOsuUser.TryGetValue(sMsg.MentionedUsers.First().Id, out userToCheck))
                    {
                        sMsg.Channel.SendMessageAsync("That person has not linked their osu! account.");
                        return;
                    }
                }

                if (indexToCheck == null)
                {
                    if (!discordChannelToBeatmap.TryGetValue(sMsg.Channel.Id, out beatmapID))
                    {
                        sMsg.Channel.SendMessageAsync("No beatmap found in conversation");
                        return;
                    }
                }
                else
                {
                    if (!discordChannelToBeatmapTop.TryGetValue(sMsg.Channel.Id, out List<BanchoAPI.BanchoBestScore> bestPlays))
                    {
                        sMsg.Channel.SendMessageAsync("No beatmap top found in conversation");
                        return;
                    }
                    else
                    {
                        indexToCheck = Math.Max(indexToCheck.Value - 1, 0);
                        beatmapID = bestPlays[Math.Min(bestPlays.Count, indexToCheck.Value)].BeatmapID;

                        if (discordChannelToBeatmap.ContainsKey(sMsg.Channel.Id))
                            discordChannelToBeatmap[sMsg.Channel.Id] = beatmapID;
                        else
                            discordChannelToBeatmap.Add(sMsg.Channel.Id, beatmapID);
                    }
                }

                if (userToCheck == "")
                {
                    if (!discordUserToOsuUser.TryGetValue(sMsg.Author.Id, out userToCheck))
                    {
                        sMsg.Channel.SendMessageAsync("Please mention someone, use their username or set your own with **>osuset <username>**");
                        return;
                    }
                }

                EmbedBuilder embedBuilder = new EmbedBuilder();

                Logger.Log($"getting user plays for '{userToCheck}'", LogLevel.Info);

                try
                {
                    List<BanchoAPI.BanchoScore> userPlays = banchoAPI.GetScores(userToCheck, beatmapID);

                    if (userPlays.Count == 0)
                    {
                        sMsg.Channel.SendMessageAsync($"**{userToCheck} doesn't have any plays on this map** <:sadChamp:593405356864962560>");
                        return;
                    }

                    List<OsuScore> kek = new List<OsuScore>();

                    for (int i = 0; i < userPlays.Count; i++)
                    {
                        var play = userPlays[i];

                        kek.Add(new OsuScore(BeatmapManager.GetBeatmap(beatmapID), play, beatmapID));
                    }

                    embedBuilder = CreateScoresEmbed(kek);

                    embedBuilder.WithThumbnailUrl(BanchoAPI.GetBeatmapImageUrl(Utils.FindBeatmapSetID(BeatmapManager.GetBeatmap(beatmapID)).ToString()));

                    embedBuilder.WithAuthor($"Plays for {userToCheck}", BanchoAPI.GetProfileImageUrl(userPlays[0].UserID.ToString()));

                    sMsg.Channel.SendMessageAsync("", false, embedBuilder.Build());
                }
                catch (Exception ex)
                {
                    Logger.Log(ex.Message, LogLevel.Warning);
                    Logger.Log(ex.StackTrace, LogLevel.Error);
                    sMsg.Channel.SendMessageAsync("uh oh something happend check console");
                }
            }, ">c", ">compare"));

            Commands.Add(new Command("Shows your osu profile or someone elses", (sMsg, buffer) =>
            {
                string userToCheck = "";

                if (sMsg.MentionedUsers.Count > 0)
                {
                    if (!discordUserToOsuUser.TryGetValue(sMsg.MentionedUsers.First().Id, out userToCheck))
                    {
                        sMsg.Channel.SendMessageAsync("That person has not linked their osu! account.");
                        return;
                    }
                }

                bool isRipple = buffer.HasParameter("-ripple");

                if (userToCheck == "")
                    userToCheck = buffer.GetRemaining();

                if (userToCheck == "")
                {
                    if (!discordUserToOsuUser.TryGetValue(sMsg.Author.Id, out userToCheck))
                    {
                        sMsg.Channel.SendMessageAsync("Please mention someone, use their username or set your own with **>osuset <username>**");
                        return;
                    }
                }

                try
                {
                    BanchoAPI.BanchoUser user = banchoAPI.GetUser(userToCheck).First();

                    OsuProfile profile = new OsuProfile(user);

                    EmbedBuilder embedBuilder = CreateProfileEmbed(profile);

                    embedBuilder.WithAuthor($"osu! Profile For {userToCheck}", BanchoAPI.GetFlagImageUrl(user.Country));

                    embedBuilder.WithThumbnailUrl(BanchoAPI.GetProfileImageUrl(user.ID.ToString()));
                    embedBuilder.WithFooter($"On {profile.Server}"); 
                    sMsg.Channel.SendMessageAsync("", false, embedBuilder.Build());
                }
                catch (Exception ex)
                {
                    Logger.Log(ex.StackTrace, LogLevel.Error);
                    sMsg.Channel.SendMessageAsync("uh oh something happend check console");
                }
            }, ">osu"));


            Commands.Add(new Command("Sets your osu user", (sMsg, buffer) =>
            {
                string[] user = sMsg.Content.Split(' ');
                if (user.Length > 1)
                {
                    discordUserToOsuUser.AddAndSave(sMsg.Author.Id, user[1], "./OsuUsers");

                    sMsg.Channel.SendMessageAsync("Your osu user has been set to: " + user[1]);
                }
                else
                {
                    sMsg.Channel.SendMessageAsync("Atleast type something like... i dunno? Your fucking osu! username?");
                }
            }, ">osuset", ">set"));
        }
    }
}
