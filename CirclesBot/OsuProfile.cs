using System;

namespace CirclesBot
{
    public class OsuProfile
    {
        public string Server { get; private set; }

        public int Playcount { get; private set; }

        public int Rank { get; private set; }

        public double Accuracy { get; private set; }

        public string Country { get; private set; }

        public int CountryRank { get; private set; }

        public int ID { get; private set; }

        public string SafeUsername { get; private set; }
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

        public OsuProfile(YL3API.PlayerStat banchoUser, YL3API.Info info)
        {
            Server = "YL3";

            Playcount = banchoUser.plays;
            Rank = banchoUser.rank;
            Accuracy = banchoUser.acc;
            Country = info.country.ToUpper();
            CountryRank = banchoUser.country_rank;
            ID = banchoUser.id;
            PP = banchoUser.pp;
            SSHCount = banchoUser.xh_count;
            SSCount = banchoUser.x_count;
            SHCount = banchoUser.sh_count;
            SCount = banchoUser.s_count;
            ACount = banchoUser.a_count;

            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            //creation_time er sekundter siden 1970
            dateTime = dateTime.AddSeconds(info.creation_time).ToLocalTime();
            JoinDate = dateTime;

            TotalPlaytimeInSeconds = banchoUser.playtime;
            RankedScore = banchoUser.rscore;

            Username = info.name;
            SafeUsername = info.safe_name;
        }
    }
}
