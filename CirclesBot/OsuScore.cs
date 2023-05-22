using System;
using System.Linq;
using System.Reflection.Metadata.Ecma335;

namespace CirclesBot
{
    public class OsuScore
    {
        public int Placement = -1;

        public DateTime Date { get; private set; }

        public ulong? ID { get; private set; }

        public int Count300 { get; private set; }
        public int Count100 { get; private set; }
        public int Count50 { get; private set; }
        public int CountMiss { get; private set; }

        public int Score { get; private set; }

        public int MaxCombo { get; private set; }

        public int MapMaxCombo { get; private set; }
        public int MapTotalHitObjects { get; private set; }

        public Mods EnabledMods { get; private set; } 

        public double PP { get; private set; }
        public double Accuracy { get; private set; }

        public bool IsFC => MaxCombo == MapMaxCombo;
        public bool IsPass => RankingLetter != "F";

        public double PP_IF_FC { get; private set; }
        public double IF_FC_Accuracy { get; private set; }

        public double CompletionPercentage { get; private set; }

        public string RankingLetter { get; private set; }

        public ulong BeatmapID { get; private set; }

        public int UserID { get; private set; }
        public string Username { get; private set; }

        public double StarRating { get; private set; }
        public double CS { get; private set; }
        public double AR { get; private set; }
        public double OD { get; private set; }
        public double HP { get; private set; }
        public double BPM { get; private set; }

        public string SongName { get; private set; }
        public string DifficultyName { get; private set; }
        public string ArtistName { get; private set; }

        public OsuScore(string beatmap, Score score, int userID)
        {
            UserID = userID;

            Date = score.play_time;
            Count300 = score.n300;
            Count100 = score.n100;
            Count50 = score.n50;
            CountMiss = score.nmiss;

            Score = score.score;

            MaxCombo = score.max_combo;

            EnabledMods = (Mods)score.mods;

            BeatmapID = (ulong)score.beatmap.id;

            RankingLetter = score.grade;

            CalculatePPAndDifficulty(beatmap);

            PP = score.pp;
        }

        public OsuScore(string beatmap, YL3BeatmapScoresResponse.Score score, ulong beatmapID)
        {
            Username = score.player_name;
            UserID = score.userid;
            Date = score.play_time;
            Count300 = score.n300;
            Count100 = score.n100;
            Count50 = score.n50;
            CountMiss = score.nmiss;

            Score = score.score;

            MaxCombo = score.max_combo;

            EnabledMods = (Mods)score.mods;

            BeatmapID = beatmapID;

            RankingLetter = score.grade;

            CalculatePPAndDifficulty(beatmap);

            PP = score.pp;
        }

        private void CalculatePPAndDifficulty(string beatmap)
        {
            double objectsEncountered = Count300 + Count100 + Count50 + CountMiss;

            //:3
            string[] bruhMods = Enum.GetValues<Mods>().Where(mod => { 
                if (mod == Mods.NM) 
                    return false;
                return EnabledMods.HasFlag(mod); 
            }).Select(mod => mod.ToString()).ToArray();

            var ppCalc = PPCalculator.Calculators.OsuCalculator.CreateCalculator(beatmap, Accuracy, (int)objectsEncountered, Score, bruhMods, CountMiss, Count50, Count100);

            var ppCalcResult = ppCalc.Calculate();

            MapMaxCombo = ppCalcResult.DifficultyAttributes.MaxCombo;
            MapTotalHitObjects = ppCalcResult.Beatmap.HitObjects.Count;

            Accuracy = ppCalcResult.Score.Accuracy;

            StarRating = ppCalcResult.DifficultyAttributes.StarRating;
            CS = ppCalcResult.Beatmap.Difficulty.CircleSize;
            AR = ppCalcResult.Beatmap.Difficulty.ApproachRate;
            OD = ppCalcResult.Beatmap.Difficulty.OverallDifficulty;
            HP = ppCalcResult.Beatmap.Difficulty.DrainRate;
            BPM = ppCalcResult.Beatmap.ControlPointInfo.BPMMaximum;


            //100 to 300 ratio
            double expectedCount100 = ((double)Count100 * MapTotalHitObjects) / objectsEncountered;

            //50 to 300 ratio
            double expectedCount50 = ((double)Count50 * MapTotalHitObjects) / objectsEncountered;

            ppCalc.Goods = (int)expectedCount100;
            ppCalc.Mehs = (int)expectedCount50;
            ppCalc.Misses = 0;
            ppCalc.Combo = MapMaxCombo;
            var ppCalcResultIfFC = ppCalc.Calculate();

            PP_IF_FC = ppCalcResultIfFC.PerformanceAttributes.Total;
            IF_FC_Accuracy = ppCalcResultIfFC.Score.Accuracy;

            SongName = ppCalcResult.Beatmap.Metadata.Title;
            DifficultyName = ppCalcResult.Beatmap.BeatmapInfo.DifficultyName;

            CompletionPercentage = (objectsEncountered / MapTotalHitObjects) * 100;
        }
    }
}
