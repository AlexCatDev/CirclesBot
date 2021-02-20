using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CirclesBot
{
    /*
    /// <summary>
    /// i dont wanna do this
    /// </summary>
    public class OsuBeatmap
    {
        
        public double RawAim { get; set; }
        public double RawSpeed { get; set; }

        public double ApproachRate;
        public double OverallDifficulty;
        public double HitPoints;
        public double CircleSize;

        public int MaxCombo;
        public int TotalHitObjects;

        public int SpinnerCount;
        public int SliderCount;
        public int HitCircleCount;
    }

    public class OsuPerformanceCalculator
    {
        private OsuBeatmap beatmap;

        private Mods mods;

        private double accuracy;
        private int scoreMaxCombo;
        private int countGreat;
        private int countOk;
        private int countMeh;
        private int countMiss;

        public OsuPerformanceCalculator(OsuBeatmap beatmap, Mods mods, int maxCombo, int count300, int count100, int count50, int countMiss, double accuracy)
        {
            this.beatmap = beatmap;
            this.mods = mods;
            this.scoreMaxCombo = maxCombo;
            this.countGreat = count300;
            this.countOk = count100;
            this.countMeh = count50;
            this.countMiss = countMiss;
        }

        public double Calculate()
        {
            // Custom multipliers for NoFail and SpunOut.
            double multiplier = 1.12; // This is being adjusted to keep the final pp value scaled around what it used to be when changing things

            if (mods == Mods.NF)
                multiplier *= Math.Max(0.90, 1.0 - 0.02 * countMiss);

            if (mods == Mods.SO)
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
            double rawAim = beatmap.RawAim;

            if (mods == Mods.TD)
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
            if (mods == Mods.HD)
                aimValue *= 1.0 + 0.04 * (12.0 - beatmap.ApproachRate);

            if (mods == Mods.FL)
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
            double speedValue = Math.Pow(5.0 * Math.Max(1.0, beatmap.RawSpeed / 0.0675) - 4.0, 3.0) / 100000.0;

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

            if (mods == Mods.HD)
                speedValue *= 1.0 + 0.04 * (12.0 - beatmap.ApproachRate);

            // Scale the speed value with accuracy and OD
            speedValue *= (0.95 + Math.Pow(beatmap.OverallDifficulty, 2) / 750) * Math.Pow(accuracy, (14.5 - Math.Max(beatmap.OverallDifficulty, 8)) / 2);
            // Scale the speed value with # of 50s to punish doubletapping.
            speedValue *= Math.Pow(0.98, countMeh < totalHits / 500.0 ? 0 : countMeh - totalHits / 500.0);

            return speedValue;
        }

        private double computeAccuracyValue()
        {
            // This percentage only considers HitCircles of any value - in this part of the calculation we focus on hitting the timing hit window
            double betterAccuracyPercentage;
            int amountHitObjectsWithAccuracy = beatmap.HitCircleCount;

            if (amountHitObjectsWithAccuracy > 0)
                betterAccuracyPercentage = ((countGreat - (totalHits - amountHitObjectsWithAccuracy)) * 6 + countOk * 2 + countMeh) / (double)(amountHitObjectsWithAccuracy * 6);
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

            if (mods == Mods.HD)
                accuracyValue *= 1.08;
            if (mods == Mods.FL)
                accuracyValue *= 1.02;

            return accuracyValue;
        }

        private int totalHits => countGreat + countOk + countMeh + countMiss;
        private int totalSuccessfulHits => countGreat + countOk + countMeh;
    }
    */
}
