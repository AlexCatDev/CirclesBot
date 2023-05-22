using Newtonsoft.Json;

using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Scoring;
using osu.Game.Online.API;
using System.Collections.Generic;
using osu.Game.Beatmaps;

namespace PPCalculator
{
    public class ScoreStatistics
    {
        [JsonProperty("ruleset_id")]
        public int RulesetId { get; set; }

        [JsonProperty("beatmap_id")]
        public int BeatmapId { get; set; }

        [JsonProperty("beatmap")]
        public string Beatmap { get; set; }

        [JsonProperty("mods")]
        public List<APIMod> Mods { get; set; }

        [JsonProperty("total_score")]
        public long Score { get; set; }

        [JsonProperty("accuracy")]
        public double Accuracy { get; set; }

        [JsonProperty("combo")]
        public int Combo { get; set; }

        [JsonProperty("statistics")]
        public Dictionary<HitResult, int> Statistics { get; set; }
    }

    public class PPResult
    {
        public IBeatmap? Beatmap { get; set; }

        [JsonProperty("score")]
        public ScoreStatistics Score { get; set; }

        [JsonProperty("performance_attributes")]
        public PerformanceAttributes PerformanceAttributes { get; set; }

        [JsonProperty("difficulty_attributes")]
        public DifficultyAttributes DifficultyAttributes { get; set; }
    }
}
