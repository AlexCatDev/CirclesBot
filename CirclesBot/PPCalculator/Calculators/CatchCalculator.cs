using osu.Game.Rulesets.Catch.Objects;
using osu.Game.Rulesets.Catch;
using osu.Game.Rulesets;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Scoring;
using System.Collections.Generic;
using System.Linq;
using System;

namespace PPCalculator.Calculators
{
    public class CatchCalculator : Calculator
    {
        public override double Accuracy { get; set; } = 100;

        public override Ruleset Ruleset => new CatchRuleset();

        public static CatchCalculator CreateCalculator(string beatmapString, double accuracy, int? combo, int score, string[] mods, int misses, int? mehs, int? goods)
        {
            return new CatchCalculator
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

        protected override int GetMaxCombo(IBeatmap beatmap) => beatmap.HitObjects.Count(h => h is Fruit)
                                                                + beatmap.HitObjects.OfType<JuiceStream>().SelectMany(j => j.NestedHitObjects).Count(h => !(h is TinyDroplet));

        protected override Dictionary<HitResult, int> GenerateHitResults(double accuracy, IBeatmap beatmap, int countMiss, int? countMeh, int? countGood)
        {
            var maxCombo = GetMaxCombo(beatmap);
            int maxTinyDroplets = beatmap.HitObjects.OfType<JuiceStream>().Sum(s => s.NestedHitObjects.OfType<TinyDroplet>().Count());
            int maxDroplets = beatmap.HitObjects.OfType<JuiceStream>().Sum(s => s.NestedHitObjects.OfType<Droplet>().Count()) - maxTinyDroplets;
            int maxFruits = beatmap.HitObjects.OfType<Fruit>().Count() + 2 * beatmap.HitObjects.OfType<JuiceStream>().Count() + beatmap.HitObjects.OfType<JuiceStream>().Sum(s => s.RepeatCount);

            // Either given or max value minus misses
            int countDroplets = countGood ?? Math.Max(0, maxDroplets - countMiss);

            // Max value minus whatever misses are left. Negative if impossible missCount
            int countFruits = maxFruits - (countMiss - (maxDroplets - countDroplets));

            // Either given or the max amount of hit objects with respect to accuracy minus the already calculated fruits and drops.
            // Negative if accuracy not feasable with missCount.
            int countTinyDroplets = countMeh ?? (int)Math.Round(accuracy * (maxCombo + maxTinyDroplets)) - countFruits - countDroplets;

            // Whatever droplets are left
            int countTinyMisses = maxTinyDroplets - countTinyDroplets;

            return new Dictionary<HitResult, int>
            {
                { HitResult.Great, countFruits },
                { HitResult.LargeTickHit, countDroplets },
                { HitResult.SmallTickHit, countTinyDroplets },
                { HitResult.SmallTickMiss, countTinyMisses },
                { HitResult.Miss, countMiss }
            };
        }

        protected override double GetAccuracy(Dictionary<HitResult, int> statistics)
        {
            double hits = statistics[HitResult.Great] + statistics[HitResult.LargeTickHit] + statistics[HitResult.SmallTickHit];
            double total = hits + statistics[HitResult.Miss] + statistics[HitResult.SmallTickMiss];

            return hits / total;
        }
    }
}
