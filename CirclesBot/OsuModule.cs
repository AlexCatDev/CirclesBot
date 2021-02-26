using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CirclesBot
{
    /// <summary>
    /// GIANT TODO LIST
    /// 1. Instead of downloading every beatmap and doing pp calculations with EZPP, use /get_beatmap endpoint for getting difficulty values.
    /// 2. Use osu!lazer pp calculator for ^
    /// 3. Remove EZPP after ^
    /// 4. Cleanup code
    /// 5. Add Ripple/Akatsuki/Gatari support lowpriority
    /// 6. Add Mania/CTB/Taiko support midpriority
    /// </summary>
    public class OsuModule : Module
    {
        public override string Name => "osu! Module";

        public override int Order => 0;

        //Maps a discord channel id to a list of osu! scores (ulong: Discord channel id), (self explainatory)
        private Dictionary<ulong, List<OsuScore>> channelToScores = new Dictionary<ulong, List<OsuScore>>();

        private BanchoAPI banchoAPI = new BanchoAPI(Program.Config.OSU_API_KEY);

        private EmbedBuilder CreateProfileEmbed(OsuProfile osuProfile)
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();
            embedBuilder.Description += $"▸ **[Profile Link](https://osu.ppy.sh/users/{osuProfile.ID}/osu)**\n";
            embedBuilder.Description += $"▸ **Official Rank:** #{osuProfile.Rank}  ({osuProfile.Country}#{osuProfile.CountryRank})\n";
            embedBuilder.Description += $"▸ **Level:** {osuProfile.Level.ToString("F2")}\n▸ **Total PP:** {osuProfile.PP.ToString("F2")}\n";
            embedBuilder.Description += $"▸ **Hit Accuracy:** {osuProfile.Accuracy.ToString("F2")}%\n▸ **Playcount:** {osuProfile.Playcount}";

            embedBuilder.WithColor(new Color(Utils.GetRandomNumber(0, 255), Utils.GetRandomNumber(0, 255), Utils.GetRandomNumber(0, 255)));
            return embedBuilder;
        }

        private Pages CreateScorePages(List<OsuScore> scores, string authorText)
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();
            Pages pages = new Pages();

            int count = 0;
            string description = "";

            OsuScore firstScore = null;

            foreach (var score in scores)
            {
                if (firstScore == null)
                    firstScore = score;

                count++;

                string tempDesc = "";

                string isFCInfo = "";

                string placementText = "";

                bool isLastScore = scores.IndexOf(score) == scores.Count - 1;

                if (score.Placement > -1)
                    placementText = $" `#{score.Placement}`";

                if (((double)score.MaxCombo / score.MapMaxCombo) < 0.994)
                    isFCInfo = $" ({score.PP_IF_FC.ToString("F2")}PP for {score.IF_FC_Accuracy.ToString("F2")}% FC)";

                tempDesc += $"**{count}.** [**{score.SongName} [{score.DifficultyName}]**]({BanchoAPI.GetBeatmapUrl(score.BeatmapID.ToString())}) **+{score.EnabledMods.ToFriendlyString()}** [{score.StarRating.ToString("F2")}★]\n";
                tempDesc += $"▸ {Utils.GetEmoteForRankLetter(score.RankingLetter)} ▸ **{score.PP.ToString("F2")}PP**{placementText}{isFCInfo} ▸ {score.Accuracy.ToString("F2")}%\n";
                tempDesc += $"▸ {score.Score} ▸ x{score.MaxCombo}/{score.MapMaxCombo} ▸ [{score.Count300}/{score.Count100}/{score.Count50}/{score.CountMiss}]\n";
                tempDesc += $"▸ **AR:** {score.AR.ToString("F1")} **OD:** {score.OD.ToString("F1")} **HP:** {score.HP.ToString("F1")} **CS:** {score.CS.ToString("F1")} ▸ **BPM:** {score.BPM.ToString("F0")}\n";

                if (score.IsPass == false)
                    tempDesc += $"▸ **Map Completion:** {score.CompletionPercentage.ToString("F2")}%\n";

                tempDesc += $"▸ Score set {Utils.FormatTime(DateTime.UtcNow - score.Date)} On {score.Server}\n";

                void AddToEmbed()
                {
                    embedBuilder.WithThumbnailUrl(BanchoAPI.GetBeatmapImageUrl(
                        Utils.FindBeatmapsetID(BeatmapManager.GetBeatmap(firstScore.BeatmapID)).ToString()));

                    embedBuilder.WithDescription(description);
                    embedBuilder.WithAuthor($"{authorText}", BanchoAPI.GetProfileImageUrl(score.UserID.ToString()));

                    embedBuilder.WithColor(new Color(Utils.GetRandomNumber(0, 255), Utils.GetRandomNumber(0, 255), Utils.GetRandomNumber(0, 255)));

                    //embedBuilder.WithFooter($"Displaying {scores.IndexOf(firstScore) + 1}-{scores.IndexOf(score) + 1}");

                    pages.AddEmbed(embedBuilder.Build());
                    embedBuilder = new EmbedBuilder();
                    firstScore = null;
                    description = "";
                }

                if (tempDesc.Length + description.Length >= 2048)
                {
                    AddToEmbed();
                }
                else
                {
                    description += tempDesc;

                    if (isLastScore)
                        AddToEmbed();
                }
            }

            return pages;
        }

        public string DecipherOsuUsername(SocketMessage sMsg, CommandBuffer buffer)
        {
            string username = "";
            if (sMsg.MentionedUsers.Count > 0)
            {
                Program.GetModule<SocialModule>().GetProfile(sMsg.MentionedUsers.First().Id, profile =>
                {
                    username = profile.OsuUsername;
                });

                if (username == "")
                {
                    sMsg.Channel.SendMessageAsync("That person has not linked their osu! account.");
                    return null;
                }
            }

            if (username == "")
                username = buffer.GetRemaining();

            if (username == "")
            {
                Program.GetModule<SocialModule>().GetProfile(sMsg.Author.Id, profile =>
                {
                    username = profile.OsuUsername;
                });

                if (username == "")
                {
                    sMsg.Channel.SendMessageAsync("Please mention someone, use their osu! username or link your own osu! account with **>osuset <username>**");
                    return null;
                }
            }

            return username;
        }

        public void RememberScores(ulong channelID, List<OsuScore> scores)
        {
            if (channelToScores.ContainsKey(channelID))
                channelToScores[channelID] = scores;
            else
                channelToScores.Add(channelID, scores);
        }

        public OsuModule()
        {
            AddCMD("You are lazy", (sMsg, buffer) =>
            {
                Program.GetModule<SocialModule>().GetProfile(sMsg.Author.Id, profile =>
                {
                    profile.IsLazy = true;
                });

                sMsg.Channel.SendMessageAsync("You can now use lazy commands.");
            }, ">iamlazy");

            AddCMD("You are not lazy", (sMsg, buffer) =>
            {
                Program.GetModule<SocialModule>().GetProfile(sMsg.Author.Id, profile =>
                {
                    profile.IsLazy = false;
                });
                sMsg.Channel.SendMessageAsync("You can now __no longer__ use lazy commands.");
            }, ">iamnotlazy");

            //Optimized
            AddCMD("Display recent scores for you or someone else", (sMsg, buffer) =>
            {
                if (sMsg.Content.StartsWith("."))
                {
                    bool isLazy = false;
                    Program.GetModule<SocialModule>().GetProfile(sMsg.Author.Id, profile =>
                    {
                        isLazy = profile.IsLazy;
                    });

                    if (isLazy == false)
                        return;
                }

                bool showList = buffer.HasParameter("-l");
                string userToCheck = DecipherOsuUsername(sMsg, buffer);

                if (userToCheck == null)
                    return;

                Logger.Log($"getting recent plays for '{userToCheck}'", LogLevel.Info);

                try
                {
                    List<BanchoAPI.BanchoRecentScore> recentUserPlays = banchoAPI.GetRecentPlays(userToCheck, showList ? 20 : 1);

                    if (recentUserPlays.Count == 0)
                    {
                        sMsg.Channel.SendMessageAsync($"**{userToCheck} doesn't have any recent plays!** <:sadChamp:593405356864962560>");
                        return;
                    }

                    List<OsuScore> scores = new List<OsuScore>();

                    foreach (var rup in recentUserPlays)
                    {
                        scores.Add(new OsuScore(BeatmapManager.GetBeatmap(rup.BeatmapID), rup));
                    }

                    RememberScores(sMsg.Channel.Id, scores);

                    Pages pages = CreateScorePages(scores, $"Recent plays for {userToCheck}");

                    PagesHandler.SendPages(sMsg.Channel, pages);
                }
                catch (Exception ex)
                {
                    Logger.Log(ex.StackTrace, LogLevel.Error);
                    sMsg.Channel.SendMessageAsync("uh oh something happend check console");
                }
            }, ">rs", ">recent", ".");

            AddCMD("Displays yours or someone elses scores on a specific map", (sMsg, buffer) =>
            {
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

                string userToCheck = DecipherOsuUsername(sMsg, buffer);
                if (userToCheck == null)
                    return;

                Logger.Log($"getting recent plays for '{userToCheck}'", LogLevel.Info);

                try
                {
                    List<BanchoAPI.BanchoScore> userPlays = banchoAPI.GetScores(userToCheck, beatmapID, 10);

                    if (userPlays.Count == 0)
                    {
                        sMsg.Channel.SendMessageAsync($"**{userToCheck} doesn't have any plays on this map!** <:sadChamp:593405356864962560>");
                        return;
                    }

                    List<OsuScore> scores = new List<OsuScore>();

                    foreach (var rup in userPlays)
                    {
                        scores.Add(new OsuScore(BeatmapManager.GetBeatmap(beatmapID), rup, beatmapID));
                    }

                    RememberScores(sMsg.Channel.Id, scores);

                    Pages pages = CreateScorePages(scores, $"Scores for {userToCheck} on {beatmapID}");

                    PagesHandler.SendPages(sMsg.Channel, pages);
                }
                catch (Exception ex)
                {
                    Logger.Log(ex.StackTrace, LogLevel.Error);
                    sMsg.Channel.SendMessageAsync("uh oh something happend check console");
                }
            }, ">scores", ">sc");

            AddCMD("Displays yours or someone elses scores", (sMsg, buffer) =>
            {
                bool showRecent = buffer.HasParameter("-r");

                bool isRipple = buffer.HasParameter("-ripple");

                bool justCheckPPCountPlays = buffer.HasParameter("-g");

                double? ppToCheck = buffer.GetDouble();

                string userToCheck = DecipherOsuUsername(sMsg, buffer);

                if (userToCheck == null)
                    return;

                EmbedBuilder embedBuilder = new EmbedBuilder();

                Logger.Log($"Getting best plays for '{userToCheck}'", LogLevel.Info);

                try
                {
                    List<BanchoAPI.BanchoBestScore> bestUserPlays = banchoAPI.GetBestPlays(userToCheck, 100);
                    bestUserPlays.Sort((x, y) => y.PP.CompareTo(x.PP));

                    List<BanchoAPI.BanchoBestScore> bestSortedUserPlays = new List<BanchoAPI.BanchoBestScore>(bestUserPlays);
                    bestSortedUserPlays.Sort((x, y) => DateTime.Compare(y.DateOfPlay, x.DateOfPlay));

                    if (bestUserPlays.Count == 0)
                    {
                        sMsg.Channel.SendMessageAsync($"**{userToCheck} doesn't have any top plays** :face_with_raised_eyebrow:");
                        return;
                    }

                    if (justCheckPPCountPlays)
                    {
                        if (ppToCheck.HasValue)
                        {
                            int count = 0;
                            foreach (var item in bestUserPlays)
                            {
                                if (item.PP >= ppToCheck.Value)
                                    count++;
                            }

                            sMsg.Channel.SendMessageAsync($"`{userToCheck}` **has {count} plays worth more than {ppToCheck.Value.ToString("F")}PP**");
                            return;
                        }
                        else
                        {
                            sMsg.Channel.SendMessageAsync($"Ex: >osutop -g 69");
                            return;
                        }
                    }

                    List<OsuScore> scores = new List<OsuScore>();
                    for (int i = 0; i < bestUserPlays.Count; i++)
                    {
                        if (i >= 10)
                            break;

                        OsuScore score;

                        if (showRecent)
                        {
                            score = new OsuScore(BeatmapManager.GetBeatmap(bestSortedUserPlays[i].BeatmapID), bestSortedUserPlays[i]);
                            score.Placement = bestUserPlays.IndexOf(bestSortedUserPlays[i]) + 1;
                        }
                        else
                        {
                            score = new OsuScore(BeatmapManager.GetBeatmap(bestUserPlays[i].BeatmapID), bestUserPlays[i]);
                            score.Placement = i + 1;
                        }
                        scores.Add(score);
                    }

                    RememberScores(sMsg.Channel.Id, scores);

                    Pages pages = CreateScorePages(scores, $"Top osu! scores for {userToCheck}");

                    PagesHandler.SendPages(sMsg.Channel, pages);
                }
                catch (Exception ex)
                {
                    Logger.Log(ex.StackTrace, LogLevel.Error);
                    sMsg.Channel.SendMessageAsync("uh oh something happend check console");
                }
            }, ">top", ">osutop");

            AddCMD("Displays the PP for an fc with a given accuracy and mods", (sMsg, buffer) =>
            {
                buffer.Discard("%");

                int? indexToCheck = (int?)buffer.GetInt();

                double? accuracy = buffer.GetDouble();

                if (accuracy == null)
                {
                    accuracy = indexToCheck;
                    indexToCheck = null;
                }

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
                        if (!channelToScores.TryGetValue(sMsg.Channel.Id, out List<OsuScore> scores))
                        {
                            sMsg.Channel.SendMessageAsync("No beatmap found in conversation");
                            return;
                        }
                        else
                        {
                            beatmapID = scores[0].BeatmapID;
                        }
                    }
                    else
                    {
                        if (!channelToScores.TryGetValue(sMsg.Channel.Id, out List<OsuScore> scores))
                        {
                            sMsg.Channel.SendMessageAsync("No beatmap top found in conversation");
                            return;
                        }
                        else
                        {
                            indexToCheck = Math.Max(indexToCheck.Value - 1, 0);
                            beatmapID = scores[Math.Min(scores.Count, indexToCheck.Value)].BeatmapID;
                        }
                    }
                }

                Mods mods = Utils.StringToMod(buffer.GetRemaining());

                Logger.Log($"calculating if pp fc for {beatmapID}", LogLevel.Info);

                try
                {
                    if (accuracy == null)
                    {
                        sMsg.Channel.SendMessageAsync("Example: >pp 98.5% HDDT");
                        return;
                    }

                    if (mods == Mods.Null)
                        mods = channelToScores[sMsg.Channel.Id][0].EnabledMods;

                    string localBeatmap = BeatmapManager.GetBeatmap(beatmapID);

                    var ez = EZPP.Calculate(localBeatmap, 0, 0, 0, 0, Mods.NM);

                    //idk how this works, but it just does
                    double estimatedCount100 = (ez.TotalHitObjects / 66.7) * (100.0 - accuracy.Value);

                    int estimatedCount100Ceil = (int)Math.Ceiling(estimatedCount100);
                    int estimatedCount100Floor = (int)Math.Floor(estimatedCount100);

                    string output = $"**+{mods.ToFriendlyString()}** on **{ez.SongName} [{ez.DifficultyName}]**\n";

                    ez = EZPP.Calculate(localBeatmap, ez.MaxCombo, estimatedCount100Ceil, 0, 0, mods);
                    output += $"**{ez.Accuracy.ToString("F2")}%** ▸ **{ez.PP.ToString("F2")}PP**\n";
                    ez = EZPP.Calculate(localBeatmap, ez.MaxCombo, estimatedCount100Floor, 0, 0, mods);
                    output += $"**{ez.Accuracy.ToString("F2")}%** ▸ **{ez.PP.ToString("F2")}PP**";

                    sMsg.Channel.SendMessageAsync(output);
                }
                catch (Exception ex)
                {
                    Logger.Log(ex.Message, LogLevel.Warning);
                    Logger.Log(ex.StackTrace, LogLevel.Error);
                    sMsg.Channel.SendMessageAsync("uh oh something happend check console");
                }
            }, ">pp", ">fc");

            AddCMD("Compare yours or someone elses scores", (sMsg, buffer) =>
            {
                if (sMsg.Content.StartsWith(","))
                {
                    bool isLazy = false;
                    Program.GetModule<SocialModule>().GetProfile(sMsg.Author.Id, profile =>
                    {
                        isLazy = profile.IsLazy;
                    });

                    if (isLazy == false)
                        return;
                }

                bool isRipple = buffer.HasParameter("-ripple");

                int? indexToCheck = (int?)buffer.GetInt();

                ulong beatmapID = 0;

                string userToCheck = DecipherOsuUsername(sMsg, buffer);

                if (userToCheck == null)
                    return;

                if (indexToCheck == null)
                {
                    if (!channelToScores.TryGetValue(sMsg.Channel.Id, out List<OsuScore> scores))
                    {
                        sMsg.Channel.SendMessageAsync("No beatmap found in conversation");
                        return;
                    }
                    else
                    {
                        beatmapID = scores[0].BeatmapID;
                    }
                }
                else
                {
                    if (!channelToScores.TryGetValue(sMsg.Channel.Id, out List<OsuScore> scores))
                    {
                        sMsg.Channel.SendMessageAsync("No beatmap top found in conversation");
                        return;
                    }
                    else
                    {
                        indexToCheck = Math.Max(indexToCheck.Value - 1, 0);
                        var score = scores[Math.Min(scores.Count - 1, indexToCheck.Value)];

                        beatmapID = score.BeatmapID;
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

                    List<OsuScore> scores = new List<OsuScore>();

                    for (int i = 0; i < userPlays.Count; i++)
                    {
                        var play = userPlays[i];

                        scores.Add(new OsuScore(BeatmapManager.GetBeatmap(beatmapID), play, beatmapID));
                    }

                    Pages pages = CreateScorePages(scores, $"Scores for {userToCheck} on {beatmapID}");

                    PagesHandler.SendPages(sMsg.Channel, pages);
                }
                catch (Exception ex)
                {
                    Logger.Log(ex.Message, LogLevel.Warning);
                    Logger.Log(ex.StackTrace, LogLevel.Error);
                    sMsg.Channel.SendMessageAsync("uh oh something happend check console");
                }
            }, ">c", ">compare", ",");

            AddCMD("Shows your osu profile or someone elses", (sMsg, buffer) =>
            {
                bool isRipple = buffer.HasParameter("-ripple");

                string userToCheck = DecipherOsuUsername(sMsg, buffer);

                if (userToCheck == null)
                    return;

                try
                {
                    BanchoAPI.BanchoUser user = banchoAPI.GetUser(userToCheck).First();

                    OsuProfile profile = new OsuProfile(user);

                    EmbedBuilder embedBuilder = CreateProfileEmbed(profile);

                    embedBuilder.WithAuthor($"osu! Profile For {userToCheck}", BanchoAPI.GetFlagImageUrl(user.Country));

                    embedBuilder.WithThumbnailUrl(BanchoAPI.GetProfileImageUrl(user.ID.ToString()));
                    embedBuilder.WithFooter($"On {profile.Server} Server");
                    sMsg.Channel.SendMessageAsync("", false, embedBuilder.Build());
                }
                catch (Exception ex)
                {
                    Logger.Log(ex.StackTrace, LogLevel.Error);
                    sMsg.Channel.SendMessageAsync("uh oh something happend check console");
                }
            }, ">osu");

            AddCMD("Links your osu! account to the bot", (sMsg, buffer) =>
            {
                string[] user = sMsg.Content.Split(' ');
                if (user.Length > 1)
                {
                    Program.GetModule<SocialModule>().GetProfile(sMsg.Author.Id, profile =>
                    {
                        profile.OsuUsername = user[1];
                    });

                    sMsg.Channel.SendMessageAsync("Your osu user has been set to: " + user[1]);
                }
                else
                {
                    sMsg.Channel.SendMessageAsync("Atleast type something like... i dunno? Your fucking **osu!** username?");
                }
            }, ">osuset", ">set");
        }
    }
}
