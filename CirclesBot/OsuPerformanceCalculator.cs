using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CirclesBot
{
    public enum OsuMode
    {
        Standard,
        Taiko,
        CTB,
        Mania
    }

    public class OsuBeatmap
    {
        [JsonProperty("approved")]
        public int Approved { get; set; }

        [JsonProperty("submit_date")]
        public DateTime SubmitDate { get; set; }

        [JsonProperty("approved_date")]
        public DateTime ApprovedDate { get; set; }

        [JsonProperty("last_update")]
        public DateTime LastUpdate { get; set; }

        [JsonProperty("artist")]
        public string Artist { get; set; }

        [JsonProperty("beatmap_id")]
        public ulong BeatmapID { get; set; }

        [JsonProperty("beatmapset_id")]
        public ulong BeatmapsetID { get; set; }

        [JsonProperty("bpm")]
        public int BPM { get; set; }

        [JsonProperty("creator")]
        public string Creator { get; set; }

        [JsonProperty("creator_id")]
        public ulong CreatorID { get; set; }

        [JsonProperty("difficultyrating")]
        public double StarRating { get; set; }

        [JsonProperty("diff_aim")]
        public double AimStarRating { get; set; }

        [JsonProperty("diff_speed")]
        public double SpeedStarRating { get; set; }

        [JsonProperty("diff_size")]
        public double CircleSize { get; set; }

        [JsonProperty("diff_overall")]
        public double OverallDifficulty { get; set; }

        [JsonProperty("diff_approach")]
        public double ApproachRate { get; set; }

        [JsonProperty("diff_drain")]
        public double Hitpoints { get; set; }

        [JsonProperty("hit_length")]
        public int HitLength { get; set; }

        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("genre_id")]
        public ulong GenreID { get; set; }

        [JsonProperty("language_id")]
        public ulong LanguageID { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("total_length")]
        public int TotalLength { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("file_md5")]
        public string FileMD5 { get; set; }

        [JsonProperty("mode")]
        public OsuMode Mode { get; set; }

        [JsonProperty("tags")]
        public string Tags { get; set; }

        [JsonProperty("favourite_count")]
        public int FavouriteCount { get; set; }

        [JsonProperty("rating")]
        public double UserRating { get; set; }

        [JsonProperty("playcount")]
        public int PlayCount { get; set; }

        [JsonProperty("passcount")]
        public int PassCount { get; set; }

        [JsonProperty("count_normal")]
        public int HitCircleCount { get; set; }

        [JsonProperty("count_slider")]
        public int SliderCount { get; set; }

        [JsonProperty("count_spinner")]
        public int SpinnerCount { get; set; }

        [JsonProperty("max_combo")]
        public int MaxCombo { get; set; }

        [JsonProperty("storyboard")]
        public bool HasStoryboard { get; set; }

        [JsonProperty("video")]
        public bool HasVideo { get; set; }

        [JsonProperty("download_unavailable")]
        public bool IsDownloadUnavailable { get; set; }

        [JsonProperty("audio_unavailable")]
        public bool IsAudioUnavailable { get; set; }
    }
    
    public class OsuPerformanceCalculator
    {
        private OsuBeatmap beatmap;

        private Mods mods;

        private double accuracy;
        private int scoreMaxCombo;
        private int count300;
        private int count100;
        private int count50;
        private int countMiss;

        public OsuPerformanceCalculator(OsuBeatmap beatmap, Mods mods, int maxCombo, int count300, int count100, int count50, int countMiss, double accuracy)
        {
            this.beatmap = beatmap;
            this.mods = mods;
            this.scoreMaxCombo = maxCombo;
            this.count300 = count300;
            this.count100 = count100;
            this.count50 = count50;
            this.countMiss = countMiss;
        }

        public double Calculate()
        {
            // Custom multipliers for NoFail and SpunOut.
            double multiplier = 1.12; // This is being adjusted to keep the final pp value scaled around what it used to be when changing things

            if (mods.HasFlag(Mods.NF))
                multiplier *= Math.Max(0.90, 1.0 - 0.02 * countMiss);

            if (mods.HasFlag(Mods.SO))
                multiplier *= 1.0 - Math.Pow((double)beatmap.SpinnerCount / totalHits, 0.85);

            double aimValue = computeAimValue();
            double speedValue = computeSpeedValue();
            double accuracyValue = computeAccuracyValue();
            double totalValue =
                Math.Pow(
                    Math.Pow(aimValue, 1.1) +
                    Math.Pow(speedValue, 1.1) +
                    Math.Pow(accuracyValue, 1.1), 1.0 / 1.1
                ) * multiplier;

            return totalValue;
        }

        private double computeAimValue()
        {
            double rawAim = beatmap.AimStarRating;

            if (mods.HasFlag(Mods.TD))
                rawAim = Math.Pow(rawAim, 0.8);

            double aimValue = Math.Pow(5.0 * Math.Max(1.0, rawAim / 0.0675) - 4.0, 3.0) / 100000.0;

            // Longer maps are worth more
            double lengthBonus = 0.95 + 0.4 * Math.Min(1.0, totalHits / 2000.0) +
                                 (totalHits > 2000 ? Math.Log10(totalHits / 2000.0) * 0.5 : 0.0);

            aimValue *= lengthBonus;

            // Penalize misses by assessing # of misses relative to the total # of objects. Default a 3% reduction for any # of misses.
            if (countMiss > 0)
                aimValue *= 0.97 * Math.Pow(1 - Math.Pow((double)countMiss / totalHits, 0.775), countMiss);

            // Combo scaling
            if (beatmap.MaxCombo > 0)
                aimValue *= Math.Min(Math.Pow(scoreMaxCombo, 0.8) / Math.Pow(beatmap.MaxCombo, 0.8), 1.0);

            double approachRateFactor = 0.0;
            if (beatmap.ApproachRate > 10.33)
                approachRateFactor += 0.4 * (beatmap.ApproachRate - 10.33);
            else if (beatmap.ApproachRate < 8.0)
                approachRateFactor += 0.01 * (8.0 - beatmap.ApproachRate);

            aimValue *= 1.0 + Math.Min(approachRateFactor, approachRateFactor * (totalHits / 1000.0));

            // We want to give more reward for lower AR when it comes to aim and HD. This nerfs high AR and buffs lower AR.
            if (mods.HasFlag(Mods.HD))
                aimValue *= 1.0 + 0.04 * (12.0 - beatmap.ApproachRate);

            if (mods.HasFlag(Mods.FL))
            {
                // Apply object-based bonus for flashlight.
                aimValue *= 1.0 + 0.35 * Math.Min(1.0, totalHits / 200.0) +
                            (totalHits > 200
                                ? 0.3 * Math.Min(1.0, (totalHits - 200) / 300.0) +
                                  (totalHits > 500 ? (totalHits - 500) / 1200.0 : 0.0)
                                : 0.0);
            }

            // Scale the aim value with accuracy _slightly_
            aimValue *= 0.5 + accuracy / 2.0;
            // It is important to also consider accuracy difficulty when doing that
            aimValue *= 0.98 + Math.Pow(beatmap.OverallDifficulty, 2) / 2500;

            return aimValue;
        }

        private double computeSpeedValue()
        {
            double speedValue = Math.Pow(5.0 * Math.Max(1.0, beatmap.SpeedStarRating / 0.0675) - 4.0, 3.0) / 100000.0;

            // Longer maps are worth more
            double lengthBonus = 0.95 + 0.4 * Math.Min(1.0, totalHits / 2000.0) +
                                 (totalHits > 2000 ? Math.Log10(totalHits / 2000.0) * 0.5 : 0.0);
            speedValue *= lengthBonus;

            // Penalize misses by assessing # of misses relative to the total # of objects. Default a 3% reduction for any # of misses.
            if (countMiss > 0)
                speedValue *= 0.97 * Math.Pow(1 - Math.Pow((double)countMiss / totalHits, 0.775), Math.Pow(countMiss, .875));

            // Combo scaling
            if (beatmap.MaxCombo > 0)
                speedValue *= Math.Min(Math.Pow(scoreMaxCombo, 0.8) / Math.Pow(beatmap.MaxCombo, 0.8), 1.0);

            double approachRateFactor = 0.0;
            if (beatmap.ApproachRate > 10.33)
                approachRateFactor += 0.4 * (beatmap.ApproachRate - 10.33);

            speedValue *= 1.0 + Math.Min(approachRateFactor, approachRateFactor * (totalHits / 1000.0));

            if (mods.HasFlag(Mods.HD))
                speedValue *= 1.0 + 0.04 * (12.0 - beatmap.ApproachRate);

            // Scale the speed value with accuracy and OD
            speedValue *= (0.95 + Math.Pow(beatmap.OverallDifficulty, 2) / 750) * Math.Pow(accuracy, (14.5 - Math.Max(beatmap.OverallDifficulty, 8)) / 2);
            // Scale the speed value with # of 50s to punish doubletapping.
            speedValue *= Math.Pow(0.98, count50 < totalHits / 500.0 ? 0 : count50 - totalHits / 500.0);

            return speedValue;
        }

        private double computeAccuracyValue()
        {
            // This percentage only considers HitCircles of any value - in this part of the calculation we focus on hitting the timing hit window
            double betterAccuracyPercentage;
            int amountHitObjectsWithAccuracy = beatmap.HitCircleCount;

            if (amountHitObjectsWithAccuracy > 0)
                betterAccuracyPercentage = ((count300 - (totalHits - amountHitObjectsWithAccuracy)) * 6 + count100 * 2 + count50) / (double)(amountHitObjectsWithAccuracy * 6);
            else
                betterAccuracyPercentage = 0;

            // It is possible to reach a negative accuracy with this formula. Cap it at zero - zero points
            if (betterAccuracyPercentage < 0)
                betterAccuracyPercentage = 0;

            // Lots of arbitrary values from testing.
            // Considering to use derivation from perfect accuracy in a probabilistic manner - assume normal distribution
            double accuracyValue = Math.Pow(1.52163, beatmap.OverallDifficulty) * Math.Pow(betterAccuracyPercentage, 24) * 2.83;

            // Bonus for many hitcircles - it's harder to keep good accuracy up for longer
            accuracyValue *= Math.Min(1.15, Math.Pow(amountHitObjectsWithAccuracy / 1000.0, 0.3));

            if (mods.HasFlag(Mods.HD))
                accuracyValue *= 1.08;
            if (mods.HasFlag(Mods.FL))
                accuracyValue *= 1.02;

            return accuracyValue;
        }

        private int totalHits => count300 + count100 + count50 + countMiss;
        private int totalSuccessfulHits => count300 + count100 + count50;
    }
    
}
