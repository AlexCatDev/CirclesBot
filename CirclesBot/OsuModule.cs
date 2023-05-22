using Discord;
using Discord.WebSocket;
using Microsoft.Win32.SafeHandles;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

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

        private YL3API yl3API = new YL3API();

        private EmbedBuilder CreateProfileEmbed(OsuProfile osuProfile, List<Score> topPlays)
        {
            double ppStart = 0;
            double ppEnd = 0;

            if (topPlays.Count > 0)
            {
                ppStart = topPlays[0].pp;
                ppEnd = topPlays.Last().pp;
            }

            EmbedBuilder embedBuilder = new EmbedBuilder();
            embedBuilder.Description += $"▸ **Rank:** #{osuProfile.Rank}  ({osuProfile.Country}#{osuProfile.CountryRank})\n";
            embedBuilder.Description += $"▸ **Per Points:** {osuProfile.PP:F2} ({ppStart:F2} - {ppEnd:F2})\n";
            embedBuilder.Description += $"▸ **Nøjagtighed:** {osuProfile.Accuracy.ToString("F2")}%\n";
            embedBuilder.Description += $"▸ **Spille Antal:** {osuProfile.Playcount} ({Math.Ceiling(TimeSpan.FromSeconds(osuProfile.TotalPlaytimeInSeconds).TotalHours)} Timer)\n";
            embedBuilder.Description += $"▸ **Rankeret Score:** {osuProfile.RankedScore / 1000000.0:F2} Mil\n";
            embedBuilder.Description += $"▸ {Utils.GetEmoteForRankLetter("XH")} **{osuProfile.SSHCount}** {Utils.GetEmoteForRankLetter("X")} **{osuProfile.SSCount}** {Utils.GetEmoteForRankLetter("SH")} **{osuProfile.SHCount}** {Utils.GetEmoteForRankLetter("S")} **{osuProfile.SCount}** {Utils.GetEmoteForRankLetter("A")} **{osuProfile.ACount}**\n";

            embedBuilder.WithColor(new Color(Utils.GetRandomNumber(0, 255), Utils.GetRandomNumber(0, 255), Utils.GetRandomNumber(0, 255)));
            embedBuilder.WithFooter($"Oprettede sin YL3 profil for {Utils.FormatTime(DateTime.UtcNow - osuProfile.JoinDate)}");
            return embedBuilder;
        }
        const string LOGO_URL = "https://cdn.discordapp.com/attachments/591771006633246722/1073985222305120306/yl31.png";

        private Pages CreateScorePages(List<OsuScore> scores, string authorText, bool isLeaderboard = false, string actionUsername = "")
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();
            Pages pages = new Pages();
            
            int count = 0;
            string description = "";

            void CompileEmbed(bool isLastScore, OsuScore score, OsuScore firstScore)
            {
                string beatmapSetID = Utils.FindBeatmapsetID(BeatmapManager.GetBeatmap(firstScore.BeatmapID)).ToString();

                embedBuilder.WithThumbnailUrl(YL3API.GetBeatmapImageUrl(beatmapSetID).ToString());

                embedBuilder.WithDescription(description);
                embedBuilder.WithAuthor(authorText,
                   iconUrl: YL3API.GetProfileImageUrl(firstScore.UserID.ToString()),
                   url: YL3API.GetProfileUrl(firstScore.UserID.ToString()));

                embedBuilder.WithColor(new Color(Utils.GetRandomNumber(0, 255), Utils.GetRandomNumber(0, 255), Utils.GetRandomNumber(0, 255)));

                int currentScoreIndex = 0;

                currentScoreIndex = scores.IndexOf(score);

                if (isLastScore)
                    currentScoreIndex++;

                if(scores.Count == 1) embedBuilder.WithFooter($"YL3 Server", LOGO_URL); 
                else embedBuilder.WithFooter($"YL3 Server {currentScoreIndex} af {scores.Count} Scores", LOGO_URL);

                pages.AddEmbed(embedBuilder.Build());
                embedBuilder = new EmbedBuilder();
                firstScore = null;
                description = "";
            }

            OsuScore firstScore = null;

            //This is the most disgusting code i have ever written
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
                    //string leader = count == 1 ? ":crown: " : $"{count}. ";
                    string leader = "";

                    if (count == 1)
                        leader += ":crown: ";

                    leader += score.Username.ToLower() == actionUsername.ToLower() ? $":arrow_right: **`{score.Username}`** :arrow_left:" : $"**{score.Username}**";

                    tempDesc += $"{leader} **+{score.EnabledMods.ToFriendlyString()}** [{score.StarRating:F2}★]\n";
                }
                else {
                    tempDesc += $"**{count}.** [**{score.ArtistName} - {score.SongName} [{score.DifficultyName}]**]({YL3API.GetBeatmapUrl(score.BeatmapID.ToString())}) **+{score.EnabledMods.ToFriendlyString()}** [{score.StarRating.ToString("F2")}★]\n";
                }
                tempDesc += $"▸ {Utils.GetEmoteForRankLetter(score.RankingLetter)} ▸ **{score.PP.ToString("F2")}PP ▸ {score.Accuracy.ToString("F2")}% ▸ **{placementText}{isFCInfo}\n";
                tempDesc += $"▸ {score.Score} ▸ x{score.MaxCombo}/{score.MapMaxCombo} ▸ [{score.Count300}/{score.Count100}/{score.Count50}/{score.CountMiss}]\n";
                tempDesc += $"▸ **CS:** {score.CS.ToString("F1")} **OD:** {score.OD.ToString("F1")} **AR:** {score.AR.ToString("F1")} **HP:** {score.HP.ToString("F1")} ▸ **BPM:** {score.BPM.ToString("F0")}\n";

                if (score.IsPass == false)
                    tempDesc += $"▸ **Map Completion:** {score.CompletionPercentage.ToString("F2")}%\n";

                //tempDesc += $"▸ {Utils.FormatTime(DateTime.UtcNow - score.Date, true, 2)}\n";

                string scoreHyperLink = score.ID.HasValue ? $" [Link]({YL3API.GetScoreUrl(score.ID.Value.ToString())})" : string.Empty;

                tempDesc += $"▸ <t:{new DateTimeOffset(score.Date).ToUnixTimeSeconds() + 7200}:R> {scoreHyperLink}\n";

                if (tempDesc.Length + description.Length >= 1024)
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
                    sMsg.Channel.SendMessageAsync($"You don't have a linked yl3! account **>link <username>** so i'm guessing it's '**{sMsg.Author.Username}**'");
                    return sMsg.Author.Username;
                }
            }

            username = username.Replace("\"", "");

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
            /*
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
                    var profile = CoreModule.GetModule<SocialModule>().GetProfile(user.Id);

                    if (profile == null)
                        continue;

                    string workingOsuUser = profile.OsuUsername;

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

                if(indexToCheck.HasValue == false)
                RememberScores(sMsg.Channel.Id, allScores);

                var pages = CreateScorePages(allScores, $"Server leaderboard on {allScores.First().SongName} [{allScores.First().DifficultyName}] Sorted by {sortedBy} {withMods}",
                    isLeaderboard: true);

                PagesHandler.SendPages(sMsg.Channel, pages);
            }, ".leaderboard", ".lb");
            */

            AddCMD("Display map leaderboard", (sMsg, buffer) =>
            {
                OsuGamemode mode = OsuGamemode.VANILLA_OSU;

                if (buffer.HasParameter("-mania"))
                    mode = OsuGamemode.VANILLA_MANIA;
                else if (buffer.HasParameter("-taiko"))
                    mode = OsuGamemode.VANILLA_TAIKO;
                else if (buffer.HasParameter("-catch"))
                    mode = OsuGamemode.VANILLA_CATCH;
                else if (buffer.HasParameter("-ctb"))
                    mode = OsuGamemode.VANILLA_CATCH;
                else if (buffer.HasParameter("-rx") || buffer.HasParameter("-relax"))
                    mode = OsuGamemode.RELAX_OSU;
                else if (buffer.HasParameter("-ap"))
                    mode = OsuGamemode.AUTOPILOT_OSU;

                ulong? beatmapID = ParseBeatmapUrl(sMsg, buffer);

                int? indexToCheck = buffer.GetInt();

                bool sortByPP = buffer.HasParameter("-pp");
                bool sortByAcc = buffer.HasParameter("-acc");

                bool sortByLowestMisscount = buffer.HasParameter("-miss");

                bool sortByCombo = buffer.HasParameter("-combo");

                Mods mods = Utils.StringToMod(buffer.GetRemaining());

                string username = DecipherOsuUsername(sMsg, buffer);

                if (beatmapID.Value == 0 || !beatmapID.HasValue)
                {
                    if (channelToScores.TryGetValue(sMsg.Channel.Id, out List<OsuScore> aScores) == false)
                    {
                        sMsg.Channel.SendMessageAsync("No beatmap found in conversation");
                        return;
                    }
                    if (indexToCheck == null)
                        indexToCheck = 0;

                    beatmapID = aScores[(indexToCheck.Value - 1).Clamp(0, aScores.Count - 1)].BeatmapID;
                }

                try
                {
                    var leaderboard = yl3API.GetScores(beatmapID.Value, 100, mode);

                    if(leaderboard.scores?.Count() == 0)
                    {
                        sMsg.Channel.SendMessageAsync($"No scores on **{beatmapID}** found");
                        return;
                    }

                    List<OsuScore> scores = new List<OsuScore>();

                    string beatmap = BeatmapManager.GetBeatmap(beatmapID.Value);

                    foreach (var item in leaderboard.scores)
                    {
                        scores.Add(new OsuScore(beatmap, item, beatmapID.Value));
                    }

                    Pages pages = CreateScorePages(scores, $"{mode.ToString()} Leaderboard for {scores[0].SongName} - [{scores[0].DifficultyName}]", true, username);

                    PagesHandler.SendPages(sMsg.Channel, pages);
                }
                catch (Exception ex)
                {
                    Logger.Log(ex.StackTrace, LogLevel.Error);
                    sMsg.Channel.SendMessageAsync($":warning: **{ex.Message}**");
                }

            }, ".lb", ".leaderboard");
            
            //Optimized
            AddCMD("Display recent scores for you or someone else", (sMsg, buffer) =>
            {
                OsuGamemode mode = OsuGamemode.VANILLA_OSU;

                if (buffer.HasParameter("-mania"))
                    mode = OsuGamemode.VANILLA_MANIA;
                else if (buffer.HasParameter("-taiko"))
                    mode = OsuGamemode.VANILLA_TAIKO;
                else if (buffer.HasParameter("-catch"))
                    mode = OsuGamemode.VANILLA_CATCH;
                else if (buffer.HasParameter("-ctb"))
                    mode = OsuGamemode.VANILLA_CATCH;
                else if (buffer.HasParameter("-rx") || buffer.HasParameter("-relax"))
                    mode = OsuGamemode.RELAX_OSU;
                else if (buffer.HasParameter("-ap"))
                    mode = OsuGamemode.AUTOPILOT_OSU;

                bool showList = buffer.HasParameter("-l");
                bool showBest = buffer.HasParameter("-b");

                string userToCheck = DecipherOsuUsername(sMsg, buffer);

                if (userToCheck == null)
                    return;

                Logger.Log($"getting recent plays for '{userToCheck}'", LogLevel.Info);

                try
                {
                    List<OsuScore> scores = new List<OsuScore>();

                    if (showBest)
                    {
                        var best = yl3API.GetBestPlays(userToCheck, 100, mode);

                        if (best.scores?.Count > 0)
                        {
                            var bestScoresSortedByPlayTime = new List<Score>(best.scores);
                            bestScoresSortedByPlayTime.Sort((x, y) => DateTime.Compare(y.play_time, x.play_time));

                            var recentBestScore = bestScoresSortedByPlayTime[0];

                            OsuScore score = new OsuScore(BeatmapManager.GetBeatmap((ulong)recentBestScore.beatmap.id), recentBestScore, best.player.id);
                            score.Placement = best.scores.IndexOf(recentBestScore) + 1;

                            scores.Add(score);
                        }
                        else
                        {
                            sMsg.Channel.SendMessageAsync($"**{userToCheck}** does not have any top plays!");
                            return;
                        }
                    }
                    else
                    {
                        var recentScores = yl3API.GetRecentPlays(userToCheck, showList ? 10 : 1, mode);

                        if(recentScores.scores?.Count > 0)
                        {
                            foreach (var rup in recentScores.scores)
                            {
                                OsuScore score = new OsuScore(BeatmapManager.GetBeatmap((ulong)rup.beatmap.id), rup, recentScores.player.id);

                                scores.Add(score);
                            }
                        }
                        else
                        {
                            sMsg.Channel.SendMessageAsync($"**{userToCheck}** does not have any recent plays!");
                            return;
                        }
                    }

                    RememberScores(sMsg.Channel.Id, scores);

                    string bestName = showBest ? "bedste " : "";

                    Pages pages = CreateScorePages(scores, $"Seneste {bestName}{mode.ToString()} scores sat af {scores[0].Username ?? userToCheck}");

                    PagesHandler.SendPages(sMsg.Channel, pages);
                }
                catch (Exception ex)
                {
                    Logger.Log(ex.StackTrace, LogLevel.Error);
                    sMsg.Channel.SendMessageAsync($":warning: **{ex.Message}**");
                }
            }, ".rs", ".recent");
            
            /*
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
                        sMsg.Channel.SendMessageAsync($"**{userToCheck}** does not have any plays on this map!");
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
            }, ".scores", ".sc");
            */
            AddCMD("Displays yours or someone elses scores", (sMsg, buffer) =>
            {
                OsuGamemode mode = OsuGamemode.VANILLA_OSU;

                if (buffer.HasParameter("-mania"))
                    mode = OsuGamemode.VANILLA_MANIA;
                else if (buffer.HasParameter("-taiko"))
                    mode = OsuGamemode.VANILLA_TAIKO;
                else if (buffer.HasParameter("-catch"))
                    mode = OsuGamemode.VANILLA_CATCH;
                else if (buffer.HasParameter("-ctb"))
                    mode = OsuGamemode.VANILLA_CATCH;
                else if (buffer.HasParameter("-rx") || buffer.HasParameter("-relax"))
                    mode = OsuGamemode.RELAX_OSU;
                else if (buffer.HasParameter("-ap"))
                    mode = OsuGamemode.AUTOPILOT_OSU;

                bool showRecent = buffer.HasParameter("-r");

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
                    var bestPlays = yl3API.GetBestPlays(userToCheck, 100, mode);

                    if(bestPlays.scores?.Count() == 0)
                    {
                        sMsg.Channel.SendMessageAsync($"**{userToCheck}** does not have any top plays in {mode}");
                        return;
                    }

                    var bestPlaysSortedByPP = bestPlays.scores;

                    if (justCheckPPCountPlays)
                    {
                        if (ppToCheck.HasValue)
                        {
                            int count = 0;
                            foreach (var item in bestPlaysSortedByPP)
                            {
                                if (item.pp >= ppToCheck.Value)
                                    count++;
                            }

                            sMsg.Channel.SendMessageAsync($"`{userToCheck}` **has {count} plays worth more than {ppToCheck.Value.ToString("F")}PP**");
                        }
                        else
                            sMsg.Channel.SendMessageAsync($"```.osutop -g 69 <optional_name>``` will show you how many plays exceeds 69 pp value for you or someone else");

                        return;
                    }

                    //bestPlaysSortedByPP.Sort((x, y) => y.PP.CompareTo(x.PP));

                    List<Score> bestPlaysBySorting = new(bestPlaysSortedByPP);

                    if (sortByAcc)
                    {
                        bestPlaysBySorting.Sort((x, y) => y.acc.CompareTo(x.acc));
                    }
                    else if (showRecent)
                    {
                        bestPlaysBySorting.Sort((x, y) => DateTime.Compare(y.play_time, x.play_time));
                    }
                    else
                    {
                        //Lmao do nothing kek
                    }

                    List<OsuScore> scores = new List<OsuScore>();

                    const int MAX_SCORES_SHOWN = 15;

                    for (int i = 0; i < bestPlaysSortedByPP.Count; i++)
                    {
                        if (i >= MAX_SCORES_SHOWN)
                            break;

                        OsuScore score = new OsuScore(BeatmapManager.GetBeatmap((ulong)bestPlaysBySorting[i].beatmap.id), bestPlaysBySorting[i], bestPlays.player.id);
                        //This is redudant when sorting by pp since the placement is already in order but i dont care
                        score.Placement = bestPlaysSortedByPP.IndexOf(bestPlaysBySorting[i]) + 1;

                        scores.Add(score);
                    }

                    RememberScores(sMsg.Channel.Id, scores);

                    string recentText = showRecent ? "Recent " : "";
                    recentText += sortByAcc ? "Sorted By Accuracy " : "";

                    Pages pages = CreateScorePages(scores, $"{recentText}Top {mode} Scores For {userToCheck}");

                    PagesHandler.SendPages(sMsg.Channel, pages);
                }
                catch (Exception ex)
                {
                    Logger.Log(ex.StackTrace, LogLevel.Error);
                    sMsg.Channel.SendMessageAsync($":warning: **{ex.Message}**");
                }
            }, ".top", ".osutop");

            AddCMD("View YL3 Leaderboard", (sMsg, buffer) =>
            {
                OsuGamemode mode = OsuGamemode.VANILLA_OSU;

                if (buffer.HasParameter("-mania"))
                    mode = OsuGamemode.VANILLA_MANIA;
                else if (buffer.HasParameter("-taiko"))
                    mode = OsuGamemode.VANILLA_TAIKO;
                else if (buffer.HasParameter("-catch"))
                    mode = OsuGamemode.VANILLA_CATCH;
                else if (buffer.HasParameter("-ctb"))
                    mode = OsuGamemode.VANILLA_CATCH;
                else if (buffer.HasParameter("-rx") || buffer.HasParameter("-relax"))
                    mode = OsuGamemode.RELAX_OSU;
                else if (buffer.HasParameter("-ap"))
                    mode = OsuGamemode.AUTOPILOT_OSU;

                string sortMode = "pp";

                if (buffer.HasParameter("-acc"))
                    sortMode = "acc";
                else if (buffer.HasParameter("-score"))
                    sortMode = "tscore";

                try
                {
                    var leaderboard = yl3API.GetLeaderboard(limit: 100, mode: mode, sortMode: sortMode, country: null);

                    if (leaderboard.leaderboard?.Count == 0)
                    {
                        sMsg.Channel.SendMessageAsync("Ok du burde aldrig se den her besked, hvis du ser den her besked betyder det at leaderboardet er tomt. GG <@591339926017146910>");
                        return;
                    }

                    Pages pages = new Pages();
                    int countOnPage = 1;
                    List<EmbedBuilder> builders = new List<EmbedBuilder>();

                    void add(string text, string title)
                    {
                        builders.Add(new EmbedBuilder());

                        builders.Last().WithDescription(text);
                        builders.Last().WithAuthor(title, LOGO_URL, "https://yl3.dk");
                    }

                    string kek = "";
                    int count = 1;
                    foreach (var player in leaderboard.leaderboard)
                    {
                        //kek += $"**#{count++}** :flag_{player.country}: {player.name} -> `pp:` **{player.pp}** `acc:` **{player.acc:F2}%** `plays:` **{player.plays}**\n";
                        kek += $"`#{count++}` :flag_{player.country}: **{player.name}**\n`pp:` **{player.pp}** `acc:` **{player.acc:F2}%** `plays:` **{player.plays}**";

                        if (sortMode == "tscore") kek += $" `score:` **{player.tscore/1_000_000.0:F0} million**";

                        kek += "\n";

                        if (countOnPage++ == 10)
                        {
                            countOnPage = 1;

                            add(kek, $"YL3 {mode} Leaderboard sorted by {sortMode}");

                            kek = "";
                        }
                    }

                    if (!string.IsNullOrEmpty(kek))
                        add(kek, $"YL3 {mode} Leaderboard sorted by {sortMode}");


                    for (int i = 0; i < builders.Count(); i++)
                    {
                        builders[i].WithFooter($"Page {i + 1}/{builders.Count()}");
                        pages.AddEmbed(builders[i].Build());
                    }
                    
                    sMsg.Channel.SendPages(pages);

                }
                catch (Exception ex)
                {
                    Logger.Log(ex.StackTrace, LogLevel.Error);
                    sMsg.Channel.SendMessageAsync($":warning: **{ex.Message}**");
                }
            }, ".slb", ".serverlb");

            /*
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
            }, ".pp", ".fc");
            */

            AddCMD("Compare yours or someone elses scores", (sMsg, buffer) =>
            {
                OsuGamemode mode = OsuGamemode.VANILLA_OSU;

                if (buffer.HasParameter("-mania"))
                    mode = OsuGamemode.VANILLA_MANIA;
                else if (buffer.HasParameter("-taiko"))
                    mode = OsuGamemode.VANILLA_TAIKO;
                else if (buffer.HasParameter("-catch"))
                    mode = OsuGamemode.VANILLA_CATCH;
                else if (buffer.HasParameter("-ctb"))
                    mode = OsuGamemode.VANILLA_CATCH;
                else if (buffer.HasParameter("-rx") || buffer.HasParameter("-relax"))
                    mode = OsuGamemode.RELAX_OSU;
                else if (buffer.HasParameter("-ap"))
                    mode = OsuGamemode.AUTOPILOT_OSU;

                int? indexToCheck = buffer.GetInt();

                ulong beatmapID = 0;

                string userToCheck = DecipherOsuUsername(sMsg, buffer);

                if (userToCheck == null)
                    return;

                if (!channelToScores.TryGetValue(sMsg.Channel.Id, out List<OsuScore> channelScores))
                {
                    sMsg.Channel.SendMessageAsync("No beatmap found in conversation");
                    return;
                }

                if (indexToCheck == null)
                    beatmapID = channelScores[0].BeatmapID;
                else
                {
                    indexToCheck = Math.Max(indexToCheck.Value - 1, 0);
                    var score = channelScores[Math.Min(channelScores.Count - 1, indexToCheck.Value)];

                    beatmapID = score.BeatmapID;
                }

                EmbedBuilder embedBuilder = new EmbedBuilder();

                Logger.Log($"getting user plays for '{userToCheck}'", LogLevel.Info);

                try
                {
                    var beatmapPlays = yl3API.GetScores(beatmapID, 100, mode);

                    if (beatmapPlays.scores?.Count == 0)
                    {
                        sMsg.Channel.SendMessageAsync($"Beatmap does not have any plays on it");
                        return;
                    }

                    var user = yl3API.GetUser(userToCheck);

                    if(user.player == null)
                    {
                        sMsg.Channel.SendMessageAsync($"**{userToCheck}** does not exist");
                        return;
                    }

                    List<OsuScore> scores = new List<OsuScore>();

                    string beatmap = BeatmapManager.GetBeatmap(beatmapID);

                    foreach (var score in beatmapPlays.scores)
                    {
                        if(score.userid == user.player.info.id)
                            scores.Add(new OsuScore(beatmap, score, beatmapID));
                    }

                    if(scores.Count == 0)
                    {
                        sMsg.Channel.SendMessageAsync($"**{userToCheck}** does not have any plays on **{channelScores[0].ArtistName} - {channelScores[0].SongName} [{channelScores[0].DifficultyName}]**");
                        return;
                    }

                    Pages pages = CreateScorePages(scores, $"Scores for {userToCheck} on {scores[0].SongName} [{scores[0].DifficultyName}]");

                    PagesHandler.SendPages(sMsg.Channel, pages);
                }
                catch (Exception ex)
                {
                    Logger.Log(ex.Message, LogLevel.Warning);
                    Logger.Log(ex.StackTrace, LogLevel.Error);
                    sMsg.Channel.SendMessageAsync($":warning: **{ex.Message}**");
                }
            }, ".c", ".compare");
            
            AddCMD("Shows your osu profile or someone elses in the respected gamemode with -flag", (sMsg, buffer) =>
            {
                OsuGamemode mode = OsuGamemode.VANILLA_OSU;

                if (buffer.HasParameter("-mania"))
                    mode = OsuGamemode.VANILLA_MANIA;
                else if (buffer.HasParameter("-taiko"))
                    mode = OsuGamemode.VANILLA_TAIKO;
                else if (buffer.HasParameter("-catch"))
                    mode = OsuGamemode.VANILLA_CATCH;
                else if (buffer.HasParameter("-ctb"))
                    mode = OsuGamemode.VANILLA_CATCH;
                else if (buffer.HasParameter("-rx") || buffer.HasParameter("-relax"))
                    mode = OsuGamemode.RELAX_OSU;
                else if (buffer.HasParameter("-ap"))
                    mode = OsuGamemode.AUTOPILOT_OSU;

                string userToCheck = DecipherOsuUsername(sMsg, buffer);

                if (userToCheck == null)
                    return;

                try
                {
                    var player = yl3API.GetUser(userToCheck).player;

                    if(player == null)
                    {
                        sMsg.Channel.SendMessageAsync("This user doesn't exist.");
                        return;
                    }

                    var topPlays = yl3API.GetBestPlays(userToCheck, 100, mode);
                    //topPlays.Sort((x, y) => x.PP.CompareTo(y.PP));

                    YL3API.PlayerStat playerStat = null;

                    switch (mode)
                    {
                        case OsuGamemode.VANILLA_OSU:
                            playerStat = player.stats._0;
                            break;
                        case OsuGamemode.VANILLA_TAIKO:
                            playerStat = player.stats._1;
                            break;
                        case OsuGamemode.VANILLA_CATCH:
                            playerStat = player.stats._2;
                            break;
                        case OsuGamemode.VANILLA_MANIA:
                            playerStat = player.stats._3;
                            break;
                        case OsuGamemode.RELAX_OSU:
                            playerStat = player.stats._4;
                            break;
                        case OsuGamemode.RELAX_TAIKO:
                            playerStat = player.stats._5;
                            break;
                        case OsuGamemode.RELAX_CATCH:
                            playerStat = player.stats._6;
                            break;
                        case OsuGamemode.RELAX_MANIA:
                            break;
                        case OsuGamemode.AUTOPILOT_OSU:
                            playerStat = player.stats._8;
                            break;
                        case OsuGamemode.AUTOPILOT_TAIKO:
                        case OsuGamemode.AUTOPILOT_CATCH:
                        case OsuGamemode.AUTOPILOT_MANIA:
                        default:
                            break;
                    }

                    if(playerStat == null)
                    {
                        sMsg.Channel.SendMessageAsync($"**{mode}** is not currently supported");
                        return;
                    }

                    EmbedBuilder embedBuilder = CreateProfileEmbed(new OsuProfile(playerStat, player.info), topPlays.scores);

                    embedBuilder.WithAuthor($"{mode} Profil For {player.info.name}", 
                        iconUrl: YL3API.GetFlagImageUrl(player.info.country.ToUpper()),
                        url: YL3API.GetProfileUrl(playerStat.id.ToString()));

                    embedBuilder.WithThumbnailUrl(YL3API.GetProfileImageUrl(""));

                    sMsg.Channel.SendMessageAsync("", false, embedBuilder.Build());
                }
                catch (Exception ex)
                {
                    Logger.Log(ex.StackTrace, LogLevel.Error);
                    sMsg.Channel.SendMessageAsync($":warning: **{ex.Message}**");
                }
            }, ".osu");
            
            AddCMD("Links your osu! account to the bot", (sMsg, buffer) =>
            {
                string username = buffer.GetRemaining();
                var player = yl3API.GetUser(username).player;
                if(player == null)
                {
                    sMsg.Channel.SendMessageAsync("This user doesn't exist");
                    return;
                }

                    CoreModule.GetModule<SocialModule>().ModifyProfile(sMsg.Author.Id, profile =>
                    {
                        profile.OsuUsername = player.info.name;
                        profile.CountryFlag = player.info.country;
                    });

                    sMsg.Channel.SendMessageAsync($"Your osu! username has been set to **{player.info.name}**");
            }, ".osuset", ".link");

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

            }, ".refresh", ".rdl", ".f5", ".re")
            { Cooldown = 10 });
        }
    }
}
