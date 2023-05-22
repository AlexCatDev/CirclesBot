using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;

namespace CirclesBot
{
    public class Beatmap
    {
        public string md5 { get; set; }
        public int id { get; set; }
        [JsonProperty("set_id")]
        public int SetID { get; set; }
        public string artist { get; set; }
        public string title { get; set; }
        public string version { get; set; }
        public string creator { get; set; }
        public DateTime last_update { get; set; }
        public int total_length { get; set; }
        public int max_combo { get; set; }
        public int status { get; set; }
        public int plays { get; set; }
        public int passes { get; set; }
        public int mode { get; set; }
        public double bpm { get; set; }
        public double cs { get; set; }
        public double od { get; set; }
        public double ar { get; set; }
        public double hp { get; set; }
        public double diff { get; set; }
    }

    public class Player
    {
        public int id { get; set; }
        public string name { get; set; }
        public object clan { get; set; }
    }

    public class YL3GetPlayerScoresResponse
    {
        public string status { get; set; }
        public List<Score> scores { get; set; }
        public Player player { get; set; }
    }

    public class Score
    {
        public int id { get; set; }
        public int score { get; set; }
        public double pp { get; set; }
        public double acc { get; set; }
        public int max_combo { get; set; }
        public int mods { get; set; }
        public int n300 { get; set; }
        public int n100 { get; set; }
        public int n50 { get; set; }
        public int nmiss { get; set; }
        public int ngeki { get; set; }
        public int nkatu { get; set; }
        public string grade { get; set; }
        public int status { get; set; }
        public int mode { get; set; }
        public DateTime play_time { get; set; }
        public int time_elapsed { get; set; }
        public int perfect { get; set; }
        public Beatmap beatmap { get; set; }
    }

    public class YL3BeatmapScoresResponse
    {
        public class Score
        {
            public string map_md5 { get; set; }
            public int score { get; set; }
            public double pp { get; set; }
            public double acc { get; set; }
            public int max_combo { get; set; }
            public int mods { get; set; }
            public int n300 { get; set; }
            public int n100 { get; set; }
            public int n50 { get; set; }
            public int nmiss { get; set; }
            public int ngeki { get; set; }
            public int nkatu { get; set; }
            public string grade { get; set; }
            public int status { get; set; }
            public int mode { get; set; }
            public DateTime play_time { get; set; }
            public int time_elapsed { get; set; }
            public int userid { get; set; }
            public int perfect { get; set; }
            public string player_name { get; set; }
            public object clan_id { get; set; }
            public object clan_name { get; set; }
            public object clan_tag { get; set; }
        }

        public string status { get; set; }
        public List<YL3BeatmapScoresResponse.Score> scores { get; set; }
    }

    public enum OsuGamemode
    {
        VANILLA_OSU = 0,
        VANILLA_TAIKO = 1,
        VANILLA_CATCH = 2,
        VANILLA_MANIA = 3,

        RELAX_OSU = 4,
        RELAX_TAIKO = 5,
        RELAX_CATCH = 6,
        RELAX_MANIA = 7,  // unused

        AUTOPILOT_OSU = 8,
        AUTOPILOT_TAIKO = 9,  // unused
        AUTOPILOT_CATCH = 10,  // unused
        AUTOPILOT_MANIA = 11,  // unused
    }

    public class YL3API
    {
        public static string GetProfileImageUrl(string userID) => $"https://media.discordapp.net/attachments/468520640295600146/1073719528879562763/image.png";
        public static string GetBeatmapImageUrl(string beatmapSetID) => $"https://b.ppy.sh/thumb/{beatmapSetID}l.jpg";
        public static string GetFlagImageUrl(string country) => $"https://osu.ppy.sh/images/flags/{country}.png";
        public static string GetBeatmapUrl(string beatmapID) => $"https://osu.ppy.sh/b/{beatmapID}";

        public static string GetScoreUrl(string scoreID) => $"https://osu.ppy.sh/scores/osu/{scoreID}";

        public static string GetProfileUrl(string usernameOrID) => $"https://yl3.dk/u/{usernameOrID}";

        public static int TotalAPICalls = 0;

        private static HttpClient httpClient = new HttpClient();

        public YL3GetPlayerScoresResponse GetRecentPlays(string username, int limit = 1, OsuGamemode mode = OsuGamemode.VANILLA_OSU)
        {
                TotalAPICalls++;
                string json = httpClient.GetAsync($"https://api.yl3.dk/v1/get_player_scores?scope=recent&name={username}&include_loved=true&limit={limit}&mode={(int)mode}").Result.Content.ReadAsStringAsync().Result;

            return JsonConvert.DeserializeObject<YL3GetPlayerScoresResponse>(json);
        }

        public YL3GetUserResponse GetUser(string username)
        {
                TotalAPICalls++;
                string json = httpClient.GetAsync($"https://api.yl3.dk/v1/get_player_info?name={username}&scope=all").Result.Content.ToString();
                return JsonConvert.DeserializeObject<YL3GetUserResponse>(json);
        }

        public YL3GetPlayerScoresResponse GetBestPlays(string username, int limit = 100, OsuGamemode mode = OsuGamemode.VANILLA_OSU)
        {
                TotalAPICalls++;
                string json = httpClient.GetAsync($"https://api.yl3.dk/v1/get_player_scores?scope=best&name={username}&include_loved=true&limit={limit}&mode={(int)mode}").Result.Content.ReadAsStringAsync().Result;
            return JsonConvert.DeserializeObject<YL3GetPlayerScoresResponse>(json);
        }


        public YL3BeatmapScoresResponse GetScores(ulong beatmapID, int limit = 10, OsuGamemode mode = OsuGamemode.VANILLA_OSU)
        {
                TotalAPICalls++;
                string json = httpClient.GetAsync($"https://api.yl3.dk/v1/get_map_scores?scope=best&id={beatmapID}&limit={limit}&mode={(int)mode}").Result.Content.ReadAsStringAsync().Result;
            return JsonConvert.DeserializeObject<YL3BeatmapScoresResponse>(json);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="limit"></param>
        /// <param name="mode"></param>
        /// <param name="sortMode">sort: Literal["tscore", "rscore", "pp", "acc", "plays", "playtime"] = "pp"</param>
        /// <returns></returns>
        public YL3LeaderboardResponse GetLeaderboard(int limit = 10, OsuGamemode mode = OsuGamemode.VANILLA_OSU, string sortMode = "pp", string country = null)
        {
            //https://api.yl3.dk/v1/get_leaderboard?sort=pp&mode=0&limit=20&country=dk
            TotalAPICalls++;
            string url = $"https://api.yl3.dk/v1/get_leaderboard?sort={sortMode}&mode={(int)mode}&limit={limit}" + (string.IsNullOrEmpty(country) ? "" : $"&country={country}");
            string json = httpClient.GetAsync(url).Result.Content.ReadAsStringAsync().Result;

            return JsonConvert.DeserializeObject<YL3LeaderboardResponse>(json);
        }

        public class YL3LeaderboardResponse
        {
            public class LeaderboardPlayer
            {
                public int player_id { get; set; }
                public string name { get; set; }
                public string country { get; set; }
                public long? tscore { get; set; }
                public long rscore { get; set; }
                public int pp { get; set; }
                public int plays { get; set; }
                public int playtime { get; set; }
                public double acc { get; set; }
                public int max_combo { get; set; }
                public int xh_count { get; set; }
                public int x_count { get; set; }
                public int sh_count { get; set; }
                public int s_count { get; set; }
                public int a_count { get; set; }
                public int? clan_id { get; set; }
                public string clan_name { get; set; }
                public string clan_tag { get; set; }
            }

            public string status { get; set; }
            public List<LeaderboardPlayer> leaderboard { get; set; }
        }



        public class PlayerStat
        {
            public int id { get; set; }
            public long tscore { get; set; }
            public long rscore { get; set; }
            public int pp { get; set; }
            public int plays { get; set; }
            public int playtime { get; set; }
            public double acc { get; set; }
            public int max_combo { get; set; }
            public int total_hits { get; set; }
            public int replay_views { get; set; }
            public int xh_count { get; set; }
            public int x_count { get; set; }
            public int sh_count { get; set; }
            public int s_count { get; set; }
            public int a_count { get; set; }
            public int rank { get; set; }
            public int country_rank { get; set; }
        }

        public class Info
        {
            public int id { get; set; }
            public string name { get; set; }
            public string safe_name { get; set; }
            public string email { get; set; }
            public int priv { get; set; }
            public string country { get; set; }
            public int silence_end { get; set; }
            public int donor_end { get; set; }
            public int creation_time { get; set; }
            public int latest_activity { get; set; }
            public int clan_id { get; set; }
            public int clan_priv { get; set; }
            public int preferred_mode { get; set; }
            public int play_style { get; set; }
            public object custom_badge_name { get; set; }
            public object custom_badge_icon { get; set; }
            public object userpage_content { get; set; }
        }

        public class Player
        {
            public Info info { get; set; }
            public Stats stats { get; set; }
        }

        public class YL3GetUserResponse
        {
            public string status { get; set; }
            public Player player { get; set; }
        }

        public class Stats
        {
            [JsonProperty("0")]
            public PlayerStat _0 { get; set; }

            [JsonProperty("1")]
            public PlayerStat _1 { get; set; }

            [JsonProperty("2")]
            public PlayerStat _2 { get; set; }

            [JsonProperty("3")]
            public PlayerStat _3 { get; set; }

            [JsonProperty("4")]
            public PlayerStat _4 { get; set; }

            [JsonProperty("5")]
            public PlayerStat _5 { get; set; }

            [JsonProperty("6")]
            public PlayerStat _6 { get; set; }

            [JsonProperty("8")]
            public PlayerStat _8 { get; set; }
        }
    }
}
