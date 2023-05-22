using osu.Game.Beatmaps;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Catch;
using osu.Game.Rulesets.Mania;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Taiko;
using osu.Game.Skinning;
using osu.Game.Utils;

using osu.Framework.Audio.Track;
using osu.Framework.Graphics.Textures;
using osu.Game.Rulesets.Osu.Mods;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace PPCalculator
{
    public static class LegacyHelper
    {
        public static Ruleset GetRulesetFromLegacyID(int id)
        {
            switch (id)
            {
                default:
                    throw new ArgumentException("Invalid ruleset ID provided.");

                case 0:
                    return new OsuRuleset();

                case 1:
                    return new TaikoRuleset();

                case 2:
                    return new CatchRuleset();

                case 3:
                    return new ManiaRuleset();
            }
        }

        public static string GetRulesetShortNameFromId(int id)
        {
            switch (id)
            {
                default:
                    throw new ArgumentException("Invalid ruleset ID provided.");

                case 0:
                    return "osu";

                case 1:
                    return "taiko";

                case 2:
                    return "fruits";

                case 3:
                    return "mania";
            }
        }

        public static Mod[] ConvertToLegacyDifficultyAdjustmentMods(Ruleset ruleset, Mod[] mods)
        {
            var beatmap = new EmptyWorkingBeatmap
            {
                BeatmapInfo =
                {
                    Ruleset = ruleset.RulesetInfo,
                    Difficulty = new BeatmapDifficulty()
                }
            };

            var allMods = ruleset.CreateAllMods().ToArray();

            var allowedMods = ModUtils.FlattenMods(
                                          ruleset.CreateDifficultyCalculator(beatmap).CreateDifficultyAdjustmentModCombinations())
                                      .Select(m => m.GetType())
                                      .Distinct()
                                      .ToHashSet();

            if (mods.Any(m => m is ModDoubleTime))
                allowedMods.Add(allMods.Single(m => m is ModNightcore).GetType());

            if (mods.Any(m => m is ModRelax))
                allowedMods.Add(allMods.Single(m => m is ModRelax).GetType());

            if (mods.Any(m => m is OsuModAutopilot))
                allowedMods.Add(allMods.Single(m => m is OsuModAutopilot).GetType());

            var result = new List<Mod>();

            var classicMod = allMods.SingleOrDefault(m => m is ModClassic);
            if (classicMod != null)
                 result.Add(classicMod);

            result.AddRange(mods.Where(m => allowedMods.Contains(m.GetType())));

            return result.ToArray();
        }

        private class EmptyWorkingBeatmap : WorkingBeatmap
        {
            public EmptyWorkingBeatmap()
                : base(new BeatmapInfo(), null)
            {
            }

            protected override IBeatmap GetBeatmap() => throw new NotImplementedException();

            protected override Texture GetBackground() => throw new NotImplementedException();

            protected override Track GetBeatmapTrack() => throw new NotImplementedException();

            protected override ISkin GetSkin() => throw new NotImplementedException();

            public override Stream GetStream(string storagePath) => throw new NotImplementedException();
        }
    }
}
