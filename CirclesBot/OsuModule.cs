using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        private BanchoAPI banchoAPI = new BanchoAPI(CoreModule.Config.OSU_API_KEY);

        private EmbedBuilder CreateProfileEmbed(OsuProfile osuProfile, List<BanchoAPI.BanchoBestScore> topPlays)
        {
            float ppStart = 0;
            float ppEnd = 0;

            if (topPlays.Count > 0)
            {
                ppStart = topPlays[0].PP;
                ppEnd = topPlays.Last().PP;
            }

            EmbedBuilder embedBuilder = new EmbedBuilder();
            embedBuilder.Description += $"▸ **[Profile Link](https://osu.ppy.sh/users/{osuProfile.ID}/osu)**\n";
            embedBuilder.Description += $"▸ **Rank:** #{osuProfile.Rank}  ({osuProfile.Country}#{osuProfile.CountryRank})\n";
            embedBuilder.Description += $"▸ **Level:** {osuProfile.Level:F2}\n";
            embedBuilder.Description += $"▸ **PP:** {osuProfile.PP:F2} ({ppStart} - {ppEnd})\n";
            embedBuilder.Description += $"▸ **Accuracy:** {osuProfile.Accuracy.ToString("F2")}%\n";
            embedBuilder.Description += $"▸ **Playcount:** {osuProfile.Playcount} ({Math.Ceiling(TimeSpan.FromSeconds(osuProfile.TotalPlaytimeInSeconds).TotalHours)} Hours)\n";
            embedBuilder.Description += $"▸ **Ranked Score:** {osuProfile.RankedScore / 1000000.0:F2} Million\n";
            embedBuilder.Description += $"▸ {Utils.GetEmoteForRankLetter("XH")} **{osuProfile.SSHCount}** {Utils.GetEmoteForRankLetter("X")} **{osuProfile.SSCount}** {Utils.GetEmoteForRankLetter("SH")} **{osuProfile.SHCount}** {Utils.GetEmoteForRankLetter("S")} **{osuProfile.SCount}** {Utils.GetEmoteForRankLetter("A")} **{osuProfile.ACount}**\n";

            embedBuilder.WithColor(new Color(Utils.GetRandomNumber(0, 255), Utils.GetRandomNumber(0, 255), Utils.GetRandomNumber(0, 255)));
            embedBuilder.WithFooter($"Joined osu! {Utils.FormatTime(DateTime.UtcNow - osuProfile.JoinDate)}");
            return embedBuilder;
        }

        private Pages CreateScorePages(List<OsuScore> scores, string authorText, bool isLeaderboard = false)
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();
            Pages pages = new Pages();

            int count = 0;
            string description = "";

            void CompileEmbed(bool isLastScore, OsuScore score, OsuScore firstScore)
            {
                embedBuilder.WithThumbnailUrl(BanchoAPI.GetBeatmapImageUrl(
                    Utils.FindBeatmapsetID(BeatmapManager.GetBeatmap(firstScore.BeatmapID)).ToString()));

                embedBuilder.WithDescription(description);
                embedBuilder.WithAuthor(authorText, BanchoAPI.GetProfileImageUrl(firstScore.UserID.ToString()));

                embedBuilder.WithColor(new Color(Utils.GetRandomNumber(0, 255), Utils.GetRandomNumber(0, 255), Utils.GetRandomNumber(0, 255)));

                int currentScoreIndex = 0;

                currentScoreIndex = scores.IndexOf(score);

                if (isLastScore)
                    currentScoreIndex++;

                embedBuilder.WithFooter($"Displaying {currentScoreIndex}/{scores.Count} Scores");

                pages.AddEmbed(embedBuilder.Build());
                embedBuilder = new EmbedBuilder();
                firstScore = null;
                description = "";
            }

            OsuScore firstScore = null;

            foreach (var score in scores)
            {
                if (firstScore is null)
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

                if (isLeaderboard)
                {
                    string leader = count == 1 ? ":crown: " : $"{count}. ";
                    tempDesc += $"**{leader} {score.Username} +{score.EnabledMods.ToFriendlyString()}** [{score.StarRating:F2}★]\n";
                }
                else
                    tempDesc += $"**{count}.** [**{score.SongName} [{score.DifficultyName}]**]({BanchoAPI.GetBeatmapUrl(score.BeatmapID.ToString())}) **+{score.EnabledMods.ToFriendlyString()}** [{score.StarRating.ToString("F2")}★]\n";

                tempDesc += $"▸ {Utils.GetEmoteForRankLetter(score.RankingLetter)} ▸ **{score.PP.ToString("F2")}PP**{placementText}{isFCInfo} ▸ {score.Accuracy.ToString("F2")}%\n";
                tempDesc += $"▸ {score.Score} ▸ x{score.MaxCombo}/{score.MapMaxCombo} ▸ [{score.Count300}/{score.Count100}/{score.Count50}/{score.CountMiss}]\n";
                tempDesc += $"▸ **AR:** {score.AR.ToString("F1")} **OD:** {score.OD.ToString("F1")} **HP:** {score.HP.ToString("F1")} **CS:** {score.CS.ToString("F1")} ▸ **BPM:** {score.BPM.ToString("F0")}\n";

                if (score.IsPass == false)
                    tempDesc += $"▸ **Map Completion:** {score.CompletionPercentage.ToString("F2")}%\n";

                tempDesc += $"▸ Score set {Utils.FormatTime(DateTime.UtcNow - score.Date)}\n";

                if (tempDesc.Length + description.Length >= 2048)
                    CompileEmbed(isLastScore, score, firstScore);

                description += tempDesc;

                if (isLastScore)
                    CompileEmbed(isLastScore, score, firstScore);
            }

            return pages;
        }

        public string DecipherOsuUsername(SocketMessage sMsg, CommandBuffer buffer)
        {
            string username = "";
            if (sMsg.MentionedUsers.Count > 0)
            {
                username = CoreModule.GetModule<SocialModule>().GetProfile(sMsg.MentionedUsers.First().Id).OsuUsername;

                if (username == "")
                {
                    sMsg.Channel.SendMessageAsync("That person has not linked their osu! account.");
                    return sMsg.MentionedUsers.First().Username;
                }
            }

            if(username == "")
                username = buffer.GetRemaining();

            if (username == "")
            {
                username = CoreModule.GetModule<SocialModule>().GetProfile(sMsg.Author.Id).OsuUsername;

                if (username == "")
                {
                    sMsg.Channel.SendMessageAsync("You don't have a linked osu! account **>link <username>**");
                    return sMsg.Author.Username;
                }
            }

            return username;
        }

        /// <summary>
        /// Attempts to parse beatmap url to a beatmap_id, if no url was present it will return 0. Will return null on error
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public ulong? ParseBeatmapUrl(SocketMessage sMsg, CommandBuffer buffer)
        {
            string url = buffer.GetParameter("https://osu.ppy.sh/");

            if (url == "")
                return 0;

            if (ulong.TryParse(url.Split('/').Last(), out ulong beatmapID))
                return beatmapID;
            else
            {
                sMsg.Channel.SendMessageAsync("Error parsing beatmap url.");
                return null;
            }
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
            AddCMD("Enable the use of '.' and ',' in place of >rs and >c", (sMsg, buffer) =>
            {
                CoreModule.GetModule<SocialModule>().ModifyProfile(sMsg.Author.Id, profile =>
                {
                    profile.IsLazy = true;
                });

                sMsg.Channel.SendMessageAsync("You can now use lazy commands.");
            }, ">iamlazy");

            AddCMD("Disable '.' and ','", (sMsg, buffer) =>
            {
                CoreModule.GetModule<SocialModule>().ModifyProfile(sMsg.Author.Id, profile =>
                {
                    profile.IsLazy = false;
                });
                sMsg.Channel.SendMessageAsync("You can now __no longer__ use lazy commands.");
            }, ">iamnotlazy");

            AddCMD("Diplays server leaderboard for map", (sMsg, buffer) =>
            {
                var users = sMsg.Channel.GetUsersAsync().FlattenAsync().GetAwaiter().GetResult();

                ulong? beatmapID = ParseBeatmapUrl(sMsg, buffer);

                int? indexToCheck = buffer.GetInt();

                bool sortByPP = buffer.HasParameter("-pp");
                bool sortByAcc = buffer.HasParameter("-acc");

                bool sortByLowestMisscount = buffer.HasParameter("-miss");

                bool sortByCombo = buffer.HasParameter("-combo");

                Mods mods = Utils.StringToMod(buffer.GetRemaining());

                if (beatmapID == null)
                    return;

                if (beatmapID == 0) {
                    if (channelToScores.TryGetValue(sMsg.Channel.Id, out List<OsuScore> aScores) == false)
                    {
                        sMsg.Channel.SendMessageAsync("No beatmap found in conversation");
                        return;
                    }
                    if (indexToCheck == null)
                        indexToCheck = 0;

                    beatmapID = aScores[(indexToCheck.Value - 1).Clamp(0, aScores.Count - 1)].BeatmapID;
                }

                List<OsuScore> allScores = new List<OsuScore>();
                foreach (var user in users)
                {
                    string workingOsuUser = CoreModule.GetModule<SocialModule>().GetProfile(user.Id).OsuUsername;

                    if (string.IsNullOrEmpty(workingOsuUser))
                        continue;

                    var scores = banchoAPI.GetScores(workingOsuUser, beatmapID.Value, 100);

                    if (scores.Count == 0)
                        continue;

                    if (sortByPP)
                        scores.Sort((x, y) => y.PP.Value.CompareTo(x.PP));
                    else if (sortByAcc)
                        scores.Sort((x, y) => y.Accuracy.CompareTo(x.Accuracy));
                    else if (sortByLowestMisscount)
                        scores.Sort((x, y) => x.CountMiss.CompareTo(y.CountMiss));
                    else if(sortByCombo)
                        scores.Sort((x, y) => y.MaxCombo.CompareTo(x.MaxCombo));

                    if (mods != Mods.Null)
                        scores.RemoveAll((s) => s.EnabledMods.HasFlag(mods) == false);

                    if(scores.Count > 0)
                        allScores.Add(new OsuScore(BeatmapManager.GetBeatmap(beatmapID.Value), scores.First(), beatmapID.Value));
                }

                if (allScores.Count == 0)
                {
                    sMsg.Channel.SendMessageAsync("No one has any scores on this map.");
                    return;
                }

                string sortedBy = "";
                string withMods = mods == Mods.Null ? "" : $"With Mods {Utils.ToFriendlyString(mods)}";

                if (sortByPP)
                {
                    sortedBy = "PP";
                    allScores.Sort((x, y) => y.PP.CompareTo(x.PP));
                }
                else if (sortByAcc)
                {
                    sortedBy = "Accuracy";
                    allScores.Sort((x, y) => y.Accuracy.CompareTo(x.Accuracy));
                }
                else if (sortByLowestMisscount)
                {
                    sortedBy = "Lowest Misscount";
                    allScores.Sort((x, y) => x.CountMiss.CompareTo(y.CountMiss));
                }
                else if (sortByCombo)
                {
                    sortedBy = "Highest Combo";
                    allScores.Sort((x, y) => y.MaxCombo.CompareTo(x.MaxCombo));
                }
                else
                {
                    sortedBy = "Score";
                    allScores.Sort((x, y) => y.Score.CompareTo(x.Score));
                }

                RememberScores(sMsg.Channel.Id, allScores);

                var pages = CreateScorePages(allScores, $"Server leaderboard on {allScores.First().SongName} [{allScores.First().DifficultyName}] Sorted by {sortedBy} {withMods}",
                    isLeaderboard: true);

                PagesHandler.SendPages(sMsg.Channel, pages);
            }, ">leaderboard", ">lb");

            //Optimized
            AddCMD("Display recent scores for you or someone else", (sMsg, buffer) =>
            {
                if (sMsg.Content.StartsWith("."))
                {
                    if (CoreModule.GetModule<SocialModule>().GetProfile(sMsg.Author.Id).IsLazy == false)
                        return;
                }

                OsuGamemode mode = OsuGamemode.Standard;

                if (buffer.HasParameter("-mania"))
                    mode = OsuGamemode.Mania;
                else if (buffer.HasParameter("-taiko"))
                    mode = OsuGamemode.Taiko;
                else if (buffer.HasParameter("-catch"))
                    mode = OsuGamemode.Catch;
                else if (buffer.HasParameter("-ctb"))
                    mode = OsuGamemode.Catch;

                bool showList = buffer.HasParameter("-l");
                string userToCheck = DecipherOsuUsername(sMsg, buffer);

                if (userToCheck == null)
                    return;

                Logger.Log($"getting recent plays for '{userToCheck}'", LogLevel.Info);

                try
                {
                    List<BanchoAPI.BanchoRecentScore> recentUserPlays = banchoAPI.GetRecentPlays(userToCheck, showList ? 10 : 1, mode);

                    if (recentUserPlays.Count == 0)
                    {
                        sMsg.Channel.SendMessageAsync($"**{userToCheck} doesn't have any recent plays!** <:sadChamp:593405356864962560>");
                        return;
                    }

                    List<OsuScore> scores = new List<OsuScore>();

                    foreach (var rup in recentUserPlays)
                    {
                        OsuScore score = new OsuScore(BeatmapManager.GetBeatmap(rup.BeatmapID), rup);
                        scores.Add(score);
                    }

                    RememberScores(sMsg.Channel.Id, scores);

                    Pages pages = CreateScorePages(scores, $"Recent {mode} plays for {userToCheck}");

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

                ulong? beatmapID = ParseBeatmapUrl(sMsg, buffer);

                if (beatmapID == null)
                {
                    return;
                }
                else if (beatmapID == 0)
                {
                    sMsg.Channel.SendMessageAsync("Please provide a beatmap url.");
                    return;
                }

                string userToCheck = DecipherOsuUsername(sMsg, buffer);

                if (userToCheck == null)
                    return;

                Logger.Log($"getting recent plays for '{userToCheck}'", LogLevel.Info);

                try
                {
                    List<BanchoAPI.BanchoScore> userPlays = banchoAPI.GetScores(userToCheck, beatmapID.Value, 10);

                    if (userPlays.Count == 0)
                    {
                        sMsg.Channel.SendMessageAsync($"**{userToCheck} doesn't have any plays on this map!** <:sadChamp:593405356864962560>");
                        return;
                    }

                    List<OsuScore> scores = new List<OsuScore>();

                    foreach (var rup in userPlays)
                    {
                        scores.Add(new OsuScore(BeatmapManager.GetBeatmap(beatmapID.Value), rup, beatmapID.Value));
                    }

                    RememberScores(sMsg.Channel.Id, scores);

                    Pages pages = CreateScorePages(scores, $"Scores for {userToCheck} on on {scores[0].SongName} [{scores[0].DifficultyName}]");

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

                bool sortByAcc = buffer.HasParameter("-acc");

                double? ppToCheck = buffer.GetDouble();

                string userToCheck = DecipherOsuUsername(sMsg, buffer);

                if (userToCheck == null)
                    return;

                EmbedBuilder embedBuilder = new EmbedBuilder();

                Logger.Log($"Getting best plays for '{userToCheck}'", LogLevel.Info);

                try
                {
                    List<BanchoAPI.BanchoBestScore> bestPlaysSortedByPP = banchoAPI.GetBestPlays(userToCheck, 100);
                    bestPlaysSortedByPP.Sort((x, y) => y.PP.CompareTo(x.PP));

                    List<BanchoAPI.BanchoBestScore> bestPlaysSortedByDate = new List<BanchoAPI.BanchoBestScore>(bestPlaysSortedByPP);
                    bestPlaysSortedByDate.Sort((x, y) => DateTime.Compare(y.DateOfPlay, x.DateOfPlay));

                    List<BanchoAPI.BanchoBestScore> bestPlaysSortedByAcc = new List<BanchoAPI.BanchoBestScore>(bestPlaysSortedByPP);
                    bestPlaysSortedByAcc.Sort((x, y) => y.Accuracy.CompareTo(x.Accuracy));

                    if (bestPlaysSortedByPP.Count == 0)
                    {
                        sMsg.Channel.SendMessageAsync($"**{userToCheck} doesn't have any top plays** :face_with_raised_eyebrow:");
                        return;
                    }

                    if (justCheckPPCountPlays)
                    {
                        if (ppToCheck.HasValue)
                        {
                            int count = 0;
                            foreach (var item in bestPlaysSortedByPP)
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
                    for (int i = 0; i < bestPlaysSortedByPP.Count; i++)
                    {
                        if (i >= 10)
                            break;

                        OsuScore score;

                        if (sortByAcc)
                        {
                            score = new OsuScore(BeatmapManager.GetBeatmap(bestPlaysSortedByAcc[i].BeatmapID), bestPlaysSortedByAcc[i]);
                            score.Placement = bestPlaysSortedByPP.IndexOf(bestPlaysSortedByAcc[i]) + 1;
                        }
                        else if (showRecent)
                        {
                            score = new OsuScore(BeatmapManager.GetBeatmap(bestPlaysSortedByDate[i].BeatmapID), bestPlaysSortedByDate[i]);
                            score.Placement = bestPlaysSortedByPP.IndexOf(bestPlaysSortedByDate[i]) + 1;
                        }
                        else
                        {
                            score = new OsuScore(BeatmapManager.GetBeatmap(bestPlaysSortedByPP[i].BeatmapID), bestPlaysSortedByPP[i]);
                            score.Placement = i + 1;
                        }
                        scores.Add(score);
                    }

                    RememberScores(sMsg.Channel.Id, scores);

                    string recentText = showRecent ? "Recent " : "";
                    recentText += sortByAcc ? "Sorted By Accuracy " : "";

                    Pages pages = CreateScorePages(scores, $"{recentText}Top osu! Scores For {userToCheck}");

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

                int? indexToCheck = buffer.GetInt();

                double? accuracy = buffer.GetDouble();

                if (accuracy == null)
                {
                    if (indexToCheck == null)
                    {
                        sMsg.Channel.SendMessageAsync("Example: >pp 98.5% HDDT");
                        return;
                    }

                    accuracy = indexToCheck;
                    indexToCheck = null;
                }

                accuracy = Math.Clamp(accuracy.Value, 0, 100);

                ulong? beatmapID = ParseBeatmapUrl(sMsg, buffer);

                if (beatmapID == null)
                    return;

                if (beatmapID == 0)
                {
                    if (channelToScores.TryGetValue(sMsg.Channel.Id, out List<OsuScore> scores) == false)
                    {
                        sMsg.Channel.SendMessageAsync("No beatmap found in conversation");
                        return;
                    }

                    if (indexToCheck == null)
                    {
                        beatmapID = scores[0].BeatmapID;
                    }
                    else
                    {
                        indexToCheck = Math.Max(indexToCheck.Value - 1, 0);
                        beatmapID = scores[Math.Min(scores.Count, indexToCheck.Value)].BeatmapID;
                    }
                }

                Mods mods = Utils.StringToMod(buffer.GetRemaining());

                Logger.Log($"calculating if pp fc for {beatmapID}", LogLevel.Info);

                try
                {
                    if (mods == Mods.Null)
                    {
                        if (channelToScores.ContainsKey(sMsg.Channel.Id))
                            mods = channelToScores[sMsg.Channel.Id][0].EnabledMods;
                        else
                            mods = Mods.NM;
                    }

                    string localBeatmap = BeatmapManager.GetBeatmap(beatmapID.Value);

                    var ez = EZPP.Calculate(localBeatmap, 0, 0, 0, 0, Mods.NM);

                    string output = $"**+{mods.ToFriendlyString()}** on **{ez.SongName} [{ez.DifficultyName}]**\n";

                    ez = EZPP.Calculate(localBeatmap, ez.MaxCombo, 0, 0, 0, mods, (float)accuracy.Value);
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
                    if(CoreModule.GetModule<SocialModule>().GetProfile(sMsg.Author.Id).IsLazy == false)
                        return;
                }

                bool isRipple = buffer.HasParameter("-ripple");

                int? indexToCheck = buffer.GetInt();

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

                    Pages pages = CreateScorePages(scores, $"Scores for {userToCheck} on {scores[0].SongName} [{scores[0].DifficultyName}]");

                    PagesHandler.SendPages(sMsg.Channel, pages);
                }
                catch (Exception ex)
                {
                    Logger.Log(ex.Message, LogLevel.Warning);
                    Logger.Log(ex.StackTrace, LogLevel.Error);
                    sMsg.Channel.SendMessageAsync("uh oh something happend check console");
                }
            }, ">c", ">compare", ",");

            AddCMD("Shows your osu profile or someone elses in the respected gamemode", (sMsg, buffer) =>
            {
                Enum.TryParse(buffer.TriggerText.Remove(0, 1), ignoreCase: true, out OsuGamemode mode);

                bool isRipple = buffer.HasParameter("-ripple");

                string userToCheck = DecipherOsuUsername(sMsg, buffer);

                if (userToCheck == null)
                    return;

                try
                {
                    var users = banchoAPI.GetUser(userToCheck, mode);

                    if(users.Count < 1)
                    {
                        sMsg.Channel.SendMessageAsync("This user doesn't exist.");
                        return;
                    }

                    var user = users.First();
                    var topPlays = banchoAPI.GetBestPlays(userToCheck, 100, mode);
                    topPlays.Sort((x, y) => x.PP.CompareTo(y.PP));

                    EmbedBuilder embedBuilder = CreateProfileEmbed(new OsuProfile(user), topPlays);

                    embedBuilder.WithAuthor($"{mode} Profile For {user.Username}", BanchoAPI.GetFlagImageUrl(user.Country));

                    embedBuilder.WithThumbnailUrl(BanchoAPI.GetProfileImageUrl(user.ID.ToString()));

                    sMsg.Channel.SendMessageAsync("", false, embedBuilder.Build());
                }
                catch (Exception ex)
                {
                    Logger.Log(ex.StackTrace, LogLevel.Error);
                    sMsg.Channel.SendMessageAsync("uh oh something happend check console");
                }
            }, ">osu", ">mania", ">catch", ">ctb", ">taiko");

            AddCMD("Links your osu! account to the bot", (sMsg, buffer) =>
            {
                string username = buffer.GetRemaining();
                var users = banchoAPI.GetUser(username, OsuGamemode.Standard);

                if(users.Count < 1)
                {
                    sMsg.Channel.SendMessageAsync("This user doesn't exist");
                }
                else
                {
                    var user = users.First();
                    CoreModule.GetModule<SocialModule>().ModifyProfile(sMsg.Author.Id, profile =>
                    {
                        profile.OsuUsername = user.Username;
                        profile.CountryFlag = user.Country;
                    });

                    sMsg.Channel.SendMessageAsync("Your osu user has been set to: " + user.Username);
                }
            }, ">osuset", ">link");

            Commands.Add(new Command("Use this command if a map is out of sync with bots version", (sMsg, buffer) =>
            {
                int? indexToCheck = buffer.GetInt();

                string beatmap = buffer.GetParameter("https://osu.ppy.sh/beatmapsets/");
                ulong beatmapSetID = 0;
                ulong beatmapID = 0;

                if (string.IsNullOrEmpty(beatmap) == false)
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
                    if (channelToScores.TryGetValue(sMsg.Channel.Id, out List<OsuScore> scores) == false)
                    {
                        sMsg.Channel.SendMessageAsync("No beatmap found in conversation");
                        return;
                    }

                    if (indexToCheck == null)
                    {
                        beatmapID = scores[0].BeatmapID;
                    }
                    else
                    {
                        indexToCheck = Math.Max(indexToCheck.Value - 1, 0);
                        beatmapID = scores[Math.Min(scores.Count, indexToCheck.Value)].BeatmapID;
                    }
                }

                BeatmapManager.GetBeatmap(beatmapID, true);
                sMsg.Channel.SendMessageAsync($"Refreshed {beatmapID} :ok_hand:");

            }, ">refresh", ">rdl", ">f5", ">re")
            { Cooldown = 10 });
        }
    }
}
