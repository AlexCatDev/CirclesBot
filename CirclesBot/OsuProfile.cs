using System;

namespace CirclesBot
{
    public class OsuProfile
    {
        public string Server { get; private set; }

        public int Playcount { get; private set; }

        public int Rank { get; private set; }

        public float Level { get; private set; }

        public float Accuracy { get; private set; }

        public string Country { get; private set; }

        public int CountryRank { get; private set; }

        public ulong ID { get; private set; }

        public string Username { get; private set; }

        public float PP { get; private set; }

        public int SSHCount { get; private set; }

        public int SSCount { get; private set; }

        public int SHCount { get; private set; }

        public int SCount { get; private set; }

        public int ACount { get; private set; }
        
        public DateTime JoinDate { get; private set; }

        public int TotalPlaytimeInSeconds { get; private set; }

        public long RankedScore { get; private set; }

        public OsuProfile(BanchoAPI.BanchoUser banchoUser)
        {
            Server = "Bancho";

            Playcount = banchoUser.Playcount;
            Rank = banchoUser.Rank;
            Level = banchoUser.Level;
            Accuracy = banchoUser.Accuracy;
            Country = banchoUser.Country;
            CountryRank = banchoUser.CountryRank;
            ID = banchoUser.ID;
            PP = banchoUser.PP;
            SSHCount = banchoUser.SSHCount;
            SSCount = banchoUser.SSCount;
            SHCount = banchoUser.SHCount;
            SCount = banchoUser.SCount;
            ACount = banchoUser.ACount;
            JoinDate = banchoUser.JoinDate;
            TotalPlaytimeInSeconds = banchoUser.TotalPlaytimeInSeconds;
            RankedScore = banchoUser.RankedScore;
            Username = banchoUser.Username;
        }
    }
}
