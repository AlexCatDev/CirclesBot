using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;

namespace CirclesBot
{
    public enum OsuGamemode
    {
        Standard = 0,
        Taiko = 1,
        Catch = 2,
        CTB = 2,
        Mania = 3
    }

    public class BanchoAPI
    {
        public static string GetProfileImageUrl(string userID) => $"https://a.ppy.sh/{userID}?{Utils.GetRandomNumber(1, Int32.MaxValue - 1)}.jpeg";
        public static string GetBeatmapImageUrl(string beatmapSetID) => $"https://b.ppy.sh/thumb/{beatmapSetID}l.jpg";
        public static string GetFlagImageUrl(string country) => $"https://osu.ppy.sh/images/flags/{country}.png";
        public static string GetBeatmapUrl(string beatmapID) => $"https://osu.ppy.sh/b/{beatmapID}";

        private string apiKey;

        public static int TotalAPICalls = 0;

        public BanchoAPI(string apiKey)
        {
            this.apiKey = apiKey;
        }

        public List<BanchoRecentScore> GetRecentPlays(string username, int limit = 1, OsuGamemode mode = OsuGamemode.Standard)
        {
            using (WebClient wc = new WebClient())
            {
                TotalAPICalls++;
                string json = wc.DownloadString($"https://osu.ppy.sh/api/get_user_recent?k={apiKey}&u={username}&m={(int)mode}&limit={limit}&type=string");
                return JsonConvert.DeserializeObject<List<BanchoRecentScore>>(json);
            }
        }

        public List<BanchoUser> GetUser(string username, OsuGamemode mode = OsuGamemode.Standard)
        {
            using (WebClient wc = new WebClient())
            {
                TotalAPICalls++;
                string json = wc.DownloadString($"https://osu.ppy.sh/api/get_user?k={apiKey}&u={username}&m={(int)mode}&type=string");
                return JsonConvert.DeserializeObject<List<BanchoUser>>(json);
            }
        }

        public List<BanchoBestScore> GetBestPlays(string username, int limit = 100, OsuGamemode mode = OsuGamemode.Standard)
        {
            using (WebClient wc = new WebClient())
            {
                TotalAPICalls++;
                string json = wc.DownloadString($"https://osu.ppy.sh/api/get_user_best?k={apiKey}&u={username}&m={(int)mode}&limit={limit}&type=string");
                return JsonConvert.DeserializeObject<List<BanchoBestScore>>(json);
            }
        }

        public List<BanchoScore> GetScores(string username, ulong beatmapID, int limit = 10, OsuGamemode mode = OsuGamemode.Standard)
        {
            using (WebClient wc = new WebClient())
            {
                string json = wc.DownloadString($"https://osu.ppy.sh/api/get_scores?k={apiKey}&b={beatmapID}&u={username}&m={(int)mode}&limit={limit}&type=string");
                return JsonConvert.DeserializeObject<List<BanchoScore>>(json);
            }
        }

        public class BanchoUser
        {
            [JsonProperty("playcount")]
            public int Playcount;

            [JsonProperty("pp_rank")]
            public int Rank;

            [JsonProperty("level")]
            public float Level;

            [JsonProperty("accuracy")]
            public float Accuracy;

            [JsonProperty("country")]
            public string Country;

            [JsonProperty("pp_country_rank")]
            public int CountryRank;

            [JsonProperty("user_id")]
            public ulong ID;

            [JsonProperty("pp_raw")]
            public float PP;

            [JsonProperty("ranked_score")]
            public long RankedScore;

            [JsonProperty("count_rank_ssh")]
            public int SSHCount;

            [JsonProperty("count_rank_ss")]
            public int SSCount;

            [JsonProperty("count_rank_sh")]
            public int SHCount;

            [JsonProperty("count_rank_s")]
            public int SCount;

            [JsonProperty("count_rank_a")]
            public int ACount;

            [JsonProperty("join_date")]
            public DateTime JoinDate;

            [JsonProperty("total_seconds_played")]
            public int TotalPlaytimeInSeconds;
        }

        public class BanchoScore
        {
            [JsonProperty("score_id")]
            public ulong ScoreID;
            [JsonProperty("score")]
            public int Score;
            [JsonProperty("username")]
            public string Username;
            [JsonProperty("count300")]
            public int Count300;
            [JsonProperty("count100")]
            public int Count100;
            [JsonProperty("count50")]
            public int Count50;
            [JsonProperty("countmiss")]
            public int CountMiss;
            [JsonProperty("maxcombo")]
            public int MaxCombo;
            [JsonProperty("countkatu")]
            public int CountKatu;
            [JsonProperty("countgeki")]
            public int CountGeki;
            [JsonProperty("perfect")]
            public int Perfect;
            [JsonProperty("enabled_mods")]
            public Mods EnabledMods;
            [JsonProperty("user_id")]
            public ulong UserID;
            [JsonProperty("date")]
            public DateTime DateOfPlay;
            [JsonProperty("rank")]
            public string RankLetter;
            [JsonProperty("pp")]
            public float? PP;
            [JsonProperty("replay_available")]
            public int ReplayAvailable;
        }

        public class BanchoBestScore
        {
            [JsonProperty("beatmap_id")]
            public ulong BeatmapID;
            [JsonProperty("score_id")]
            public ulong ScoreID;
            [JsonProperty("score")]
            public int Score;
            [JsonProperty("maxcombo")]
            public int MaxCombo;
            [JsonProperty("count50")]
            public int Count50;
            [JsonProperty("count100")]
            public int Count100;
            [JsonProperty("count300")]
            public int Count300;
            [JsonProperty("countmiss")]
            public int CountMiss;
            [JsonProperty("countkatu")]
            public int CountKatu;
            [JsonProperty("countgeki")]
            public int CountGeki;
            [JsonProperty("perfect")]
            public int Perfect;
            [JsonProperty("enabled_mods")]
            public Mods EnabledMods;
            [JsonProperty("user_id")]
            public ulong UserID;
            [JsonProperty("date")]
            public DateTime DateOfPlay;
            [JsonProperty("rank")]
            public string RankLetter;
            [JsonProperty("pp")]
            public float PP;
            [JsonProperty("replay_available")]
            public int ReplayAvailable;
        }

        public class BanchoRecentScore
        {
            [JsonProperty("beatmap_id")]
            public ulong BeatmapID;
            [JsonProperty("score")]
            public int Score;
            [JsonProperty("maxcombo")]
            public int MaxCombo;
            [JsonProperty("count50")]
            public int Count50;
            [JsonProperty("count100")]
            public int Count100;
            [JsonProperty("count300")]
            public int Count300;
            [JsonProperty("countmiss")]
            public int CountMiss;
            [JsonProperty("countkatu")]
            public int CountKatu;
            [JsonProperty("countgeki")]
            public int CountGeki;
            [JsonProperty("perfect")]
            public int Perfect;
            [JsonProperty("enabled_mods")]
            public Mods EnabledMods;
            [JsonProperty("user_id")]
            public ulong UserID;
            [JsonProperty("date")]
            public DateTime DateOfPlay;
            [JsonProperty("rank")]
            public string RankLetter;
        }
    }
}
