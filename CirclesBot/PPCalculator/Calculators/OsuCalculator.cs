using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets;
using osu.Game.Beatmaps;
using System.Collections.Generic;
using System;

namespace PPCalculator.Calculators
{
    public class OsuCalculator : Calculator
    {
        public override double Accuracy { get; set; } = 100;
        public override Ruleset Ruleset => new OsuRuleset();

        public static OsuCalculator CreateCalculator(string beatmapString, double accuracy, int? combo, int score, string[] mods, int misses, int? mehs, int? goods)
        {
            return new OsuCalculator
            {
                BeatmapString = beatmapString,
                Accuracy = accuracy,
                Combo = combo,
                Score = score,
                Mods = mods,
                Misses = misses,
                Mehs = mehs,
                Goods = goods,
            };
        }

        protected override int GetMaxCombo(IBeatmap beatmap) => beatmap.GetMaxCombo();

        protected override Dictionary<HitResult, int> GenerateHitResults(double accuracy, IBeatmap beatmap, int countMiss, int? countMeh, int? countGood)
        {
            int countGreat;

            var totalResultCount = beatmap.HitObjects.Count;

            if (countMeh != null || countGood != null)
            {
                countGreat = totalResultCount - (countGood ?? 0) - (countMeh ?? 0) - countMiss;
            }
            else
            {
                // Let Great=6, Good=2, Meh=1, Miss=0. The total should be this.
                var targetTotal = (int)Math.Round(accuracy * totalResultCount * 6);

                // Start by assuming every non miss is a meh
                // This is how much increase is needed by greats and goods
                var delta = targetTotal - (totalResultCount - countMiss);

                // Each great increases total by 5 (great-meh=5)
                countGreat = delta / 5;
                // Each good increases total by 1 (good-meh=1). Covers remaining difference.
                countGood = delta % 5;
                // Mehs are left over. Could be negative if impossible value of amountMiss chosen
                countMeh = totalResultCount - countGreat - countGood - countMiss;
            }

            return new Dictionary<HitResult, int>
            {
                { HitResult.Great, countGreat },
                { HitResult.Ok, countGood ?? 0 },
                { HitResult.Meh, countMeh ?? 0 },
                { HitResult.Miss, countMiss }
            };
        }

        protected override double GetAccuracy(Dictionary<HitResult, int> statistics)
        {
            var countGreat = statistics[HitResult.Great];
            var countGood = statistics[HitResult.Ok];
            var countMeh = statistics[HitResult.Meh];
            var countMiss = statistics[HitResult.Miss];
            var total = countGreat + countGood + countMeh + countMiss;

            return (double)((6 * countGreat) + (2 * countGood) + countMeh) / (6 * total);
        }
    }
}
