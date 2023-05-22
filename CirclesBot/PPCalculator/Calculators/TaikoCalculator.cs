using osu.Game.Rulesets.Taiko.Objects;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.Taiko;
using osu.Game.Beatmaps;
using osu.Game.Rulesets;
using System.Collections.Generic;
using System;
using System.Linq;

namespace PPCalculator.Calculators
{
    public class TaikoCalculator : Calculator
    {
        public override double Accuracy { get; set; } = 100;

        public override Ruleset Ruleset => new TaikoRuleset();

        public static TaikoCalculator CreateCalculator(string beatmapString, double accuracy, int? combo, int score, string[] mods, int misses, int? mehs, int? goods)
        {
            return new TaikoCalculator
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

        protected override int GetMaxCombo(IBeatmap beatmap) => beatmap.HitObjects.OfType<Hit>().Count();

        protected override Dictionary<HitResult, int> GenerateHitResults(double accuracy, IBeatmap beatmap, int countMiss, int? countMeh, int? countGood)
        {
            var totalResultCount = GetMaxCombo(beatmap);

            int countGreat;

            if (countGood != null)
            {
                countGreat = (int)(totalResultCount - countGood - countMiss);
            }
            else
            {
                // Let Great=2, Good=1, Miss=0. The total should be this.
                var targetTotal = (int)Math.Round(accuracy * totalResultCount * 2);

                countGreat = targetTotal - (totalResultCount - countMiss);
                countGood = totalResultCount - countGreat - countMiss;
            }

            return new Dictionary<HitResult, int>
            {
                { HitResult.Great, countGreat },
                { HitResult.Ok, (int)countGood },
                { HitResult.Meh, 0 },
                { HitResult.Miss, countMiss }
            };
        }

        protected override double GetAccuracy(Dictionary<HitResult, int> statistics)
        {
            var countGreat = statistics[HitResult.Great];
            var countGood = statistics[HitResult.Ok];
            var countMiss = statistics[HitResult.Miss];
            var total = countGreat + countGood + countMiss;

            return (double)((2 * countGreat) + countGood) / (2 * total);
        }
    }
}
