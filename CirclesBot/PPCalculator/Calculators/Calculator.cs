using JetBrains.Annotations;

using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.Mods;
using osu.Game.Online.API;
using osu.Game.Rulesets;
using osu.Game.Beatmaps;
using osu.Game.Scoring;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PPCalculator.Calculators
{
    public abstract class Calculator
    {
        public abstract Ruleset Ruleset { get; }

        [UsedImplicitly]
        public string BeatmapString { get; set; }

        [UsedImplicitly]
        public virtual double Accuracy { get; set; }

        [UsedImplicitly]
        public virtual int? Combo { get; set; }

        [UsedImplicitly]
        public virtual int Score { get; set; }

        // TODO: allow for mods enum and convert to string array ourselves?
        // XXX: peppy why
        [UsedImplicitly]
        public virtual string[] Mods { get; set; }

        [UsedImplicitly]
        public virtual int Misses { get; set; }

        // XX: 50s. keeping lazer name as i can't use numbers in variables
        [UsedImplicitly]
        public virtual int? Mehs { get; set; }

        // XX: 100s. keeping lazer name as i can't use numbers in variables
        [UsedImplicitly]
        public virtual int? Goods { get; set; }

        public virtual PPResult Calculate()
        {
            var mods = LegacyHelper.ConvertToLegacyDifficultyAdjustmentMods(Ruleset, GetMods());
            var workingBeatmap = ProcessorWorkingBeatmap.FromString(BeatmapString);
            var beatmap = workingBeatmap.GetPlayableBeatmap(Ruleset.RulesetInfo, mods);

            var statistics = GenerateHitResults(Accuracy / 100, beatmap, Misses, Mehs, Goods);
            var accuracy = GetAccuracy(statistics);
            var maxCombo = Combo ?? GetMaxCombo(beatmap);

            var difficultyCalculator = Ruleset.CreateDifficultyCalculator(workingBeatmap);
            var difficultyAttributes = difficultyCalculator.Calculate(mods);
            var performanceCalculator = Ruleset.CreatePerformanceCalculator();

            var ppAttributes = performanceCalculator?.Calculate(new ScoreInfo(beatmap.BeatmapInfo, Ruleset.RulesetInfo)
            {
                Accuracy = accuracy,
                MaxCombo = maxCombo,
                Statistics = statistics,
                Mods = mods,
                TotalScore = Score,
            }, difficultyAttributes);

            return new PPResult
            {
                Score = new ScoreStatistics
                {
                    RulesetId = Ruleset.RulesetInfo.OnlineID,
                    BeatmapId = workingBeatmap.BeatmapInfo.OnlineID,
                    Beatmap = workingBeatmap.BeatmapInfo.ToString(),
                    Mods = mods.Select(m => new APIMod(m)).ToList(),
                    Score = Score,
                    Accuracy = accuracy * 100,
                    Combo = maxCombo,
                    Statistics = statistics,
                },
                PerformanceAttributes = ppAttributes,
                DifficultyAttributes = difficultyAttributes,

                Beatmap = beatmap
            };
        }

        public string CalculateString()
        {
            var result = Calculate();

            return $"{result.PerformanceAttributes.Total}|{result.DifficultyAttributes.StarRating}";
        }

        protected Mod[] GetMods()
        {
            if (Mods == null)
                return Array.Empty<Mod>();

            var availableMods = Ruleset.CreateAllMods().ToList();
            var mods = new List<Mod>();

            foreach (var modString in Mods)
            {
                Mod modObj = availableMods.FirstOrDefault(m => string.Equals(m.Acronym, modString, StringComparison.CurrentCultureIgnoreCase));
                if (modObj == null)
                    throw new ArgumentException($"{modString} does not exist.");

                mods.Add(modObj);
            }

            return mods.ToArray();
        }

        protected abstract int GetMaxCombo(IBeatmap beatmap);
        protected virtual double GetAccuracy(Dictionary<HitResult, int> statistics) => 0;
        protected abstract Dictionary<HitResult, int> GenerateHitResults(double accuracy, IBeatmap beatmap, int countMiss, int? countMeh, int? countGood);
    }
}