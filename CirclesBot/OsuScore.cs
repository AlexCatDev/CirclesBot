using System;

namespace CirclesBot
{
    public class OsuScore
    {
        public string Server { get; private set; }

        public DateTime Date { get; private set; }

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

        public ulong UserID { get; private set; }

        public float StarRating { get; private set; }
        public float CS { get; private set; }
        public float AR { get; private set; }
        public float OD { get; private set; }
        public float HP { get; private set; }
        public float BPM { get; private set; }

        public string SongName { get; private set; }
        public string DifficultyName { get; private set; }

        public OsuScore(string beatmap, BanchoAPI.BanchoScore banchoPlay, ulong beatmapID)
        {
            Server = "Bancho";

            UserID = banchoPlay.UserID;
            Date = banchoPlay.DateOfPlay;
            Count300 = banchoPlay.Count300;
            Count100 = banchoPlay.Count100;
            Count50 = banchoPlay.Count50;
            CountMiss = banchoPlay.CountMiss;

            Score = banchoPlay.Score;

            MaxCombo = banchoPlay.MaxCombo;

            EnabledMods = banchoPlay.EnabledMods;

            RankingLetter = banchoPlay.RankLetter;

            BeatmapID = beatmapID;

            CalculatePPAndDifficulty(beatmap);
        }

        public OsuScore(string beatmap, BanchoAPI.BanchoBestScore banchoPlay)
        {
            Server = "Bancho";

            UserID = banchoPlay.UserID;
            Date = banchoPlay.DateOfPlay;
            Count300 = banchoPlay.Count300;
            Count100 = banchoPlay.Count100;
            Count50 = banchoPlay.Count50;
            CountMiss = banchoPlay.CountMiss;

            Score = banchoPlay.Score;

            MaxCombo = banchoPlay.MaxCombo;

            EnabledMods = banchoPlay.EnabledMods;

            RankingLetter = banchoPlay.RankLetter;

            BeatmapID = banchoPlay.BeatmapID;

            CalculatePPAndDifficulty(beatmap);

            PP = banchoPlay.PP;
        }

        public OsuScore(string beatmap, BanchoAPI.BanchoRecentScore banchoPlay)
        {
            Server = "Bancho";

            UserID = banchoPlay.UserID;
            Date = banchoPlay.DateOfPlay;
            Count300 = banchoPlay.Count300;
            Count100 = banchoPlay.Count100;
            Count50 = banchoPlay.Count50;
            CountMiss = banchoPlay.CountMiss;

            Score = banchoPlay.Score;

            MaxCombo = banchoPlay.MaxCombo;

            EnabledMods = banchoPlay.EnabledMods;

            BeatmapID = banchoPlay.BeatmapID;

            RankingLetter = banchoPlay.RankLetter;

            CalculatePPAndDifficulty(beatmap);
        }

        private void CalculatePPAndDifficulty(string beatmap)
        {
            EZPPResult ezpp = EZPP.Calculate(beatmap, MaxCombo, Count100, Count50, CountMiss, EnabledMods);

            MapMaxCombo = ezpp.MaxCombo;
            MapTotalHitObjects = ezpp.TotalHitObjects;

            PP = ezpp.PP;

            double objectsEncountered = Count300 + Count100 + Count50 + CountMiss;

            Accuracy = (double)(Count300 * 300.0 + Count100 * 100.0 + Count50 * 50.0) / (double)(objectsEncountered * 300.0);
            Accuracy *= 100.0;

                                      //300 to 100 ratio
            double expectedCount100 = ((double)Count100 / Count300) * MapTotalHitObjects;
                                      //300 to 50 ratio
            double expectedCount50 = ((double)Count50 / Count300) * MapTotalHitObjects;

            ezpp = EZPP.Calculate(beatmap, ezpp.MaxCombo, (int)Math.Floor(expectedCount100), (int)Math.Floor(expectedCount50), 0, EnabledMods);

            PP_IF_FC = ezpp.PP;
            IF_FC_Accuracy = ezpp.Accuracy;

            CompletionPercentage = (objectsEncountered / ezpp.TotalHitObjects) * 100;

            StarRating = ezpp.StarRating;
            CS = ezpp.CS;
            AR = ezpp.AR;
            OD = ezpp.OD;
            HP = ezpp.HP;   
            BPM = ezpp.BPM;

            SongName = ezpp.SongName;
            DifficultyName = ezpp.DifficultyName;
        }
    }
}
