using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.Mania;
using osu.Game.Rulesets.Mods;
using osu.Game.Beatmaps;
using osu.Game.Rulesets;

using System.Diagnostics;
using System;
using System.Collections.Generic;

namespace PPCalculator.Calculators
{
    public class ManiaCalculator : Calculator
    {
        public override int Score { get
            {
                Debug.Assert(score != null);
                return score.Value;
            }
        }

        private int? score { get; set; }

        public override Ruleset Ruleset => new ManiaRuleset();

        public static ManiaCalculator CreateCalculator(string beatmapString, double accuracy, int? combo, int? score, string[] mods, int misses, int? mehs, int? goods)
        {
            return new ManiaCalculator
            {
                BeatmapString = beatmapString,
                Accuracy = accuracy,
                Combo = combo,
                score = score,
                Mods = mods,
                Misses = misses,
                Mehs = mehs,
                Goods = goods,
            };
        }

        public override PPResult Calculate()
        {
            if (score == null)
            {
                double scoreMultiplier = 1;

                // Cap score depending on difficulty adjustment mods (matters for mania).
                foreach (var mod in GetMods())
                {
                    if (mod.Type == ModType.DifficultyReduction)
                        scoreMultiplier *= mod.ScoreMultiplier;
                }

                score = (int)Math.Round(1000000 * scoreMultiplier);
            }

            return base.Calculate();
        }

        protected override int GetMaxCombo(IBeatmap beatmap) => 0;

        protected override Dictionary<HitResult, int> GenerateHitResults(double accuracy, IBeatmap beatmap, int countMiss, int? countMeh, int? countGood)
        {
            var totalHits = beatmap.HitObjects.Count;

            // Only total number of hits is considered currently, so specifics don't matter
            return new Dictionary<HitResult, int>
            {
                { HitResult.Perfect, totalHits },
                { HitResult.Great, 0 },
                { HitResult.Ok, 0 },
                { HitResult.Good, 0 },
                { HitResult.Meh, 0 },
                { HitResult.Miss, 0 }
            };
        }
    }
}
