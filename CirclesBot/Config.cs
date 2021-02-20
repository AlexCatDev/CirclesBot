using System;

namespace CirclesBot
{
    public class Config
    {
        public static string GithubURL = "https://github.com/AlexCatDev/CirclesBot";

        public static string Filename = "./config.json";

        public string OSU_API_KEY = "";
        public string DISCORD_API_KEY = "";
        public string OPTIONAL_DISCORD_API_KEY = "";
        public ulong BotOwnerID = 0;
        public bool DMOwnerOnError = false;

        public void Verify()
        {
            if (String.IsNullOrEmpty(OSU_API_KEY))
            {
                throw new Exception("No osu! api key has been set");
            }

            if (String.IsNullOrEmpty(DISCORD_API_KEY))
            {
                throw new Exception("No discord api has been set");
            }

            if(BotOwnerID == 0)
            {
                throw new Exception("No bot owner id has been set");
            }
        }
    }
}
