using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CirclesBot
{
    public class UtilsModule : Module
    {
       
        public override string Name => "Utils Module";

        public override int Order => 3;

        public UtilsModule()
        {
            AddCMD("Convert hex to decimal", (sMsg, buffer) =>
            {
                try
                {
                    int val = Convert.ToInt32(buffer.GetRemaining(), 16);
                    sMsg.Channel.SendMessageAsync($"**{val}**");
                }
                catch
                {
                    sMsg.Channel.SendMessageAsync("Couldn't pass text as hex.");
                }
            }, ".hex");

            Commands.Add(new Command("Make the bot say something", (sMsg, buffer) =>
            {
                if (sMsg.MentionedRoles.Count > 0 || sMsg.MentionedUsers.Count > 0)
                {
                    sMsg.Channel.SendMessageAsync("no");
                    return;
                }

                string msg = sMsg.Content.Remove(0, 4);
                if (msg.Contains("@everyone") || msg.Contains("@here"))
                    sMsg.Channel.SendMessageAsync($"no");
                else
                    sMsg.Channel.SendMessageAsync($"{msg}");

            }, ".say")
            { IsEnabled = false });

            AddCMD("Roll a random number", (sMsg, buffer) =>
            {
                double maxRoll = 100;

                if (double.TryParse(buffer.TakeFirst(), out maxRoll))
                {
                    maxRoll = Math.Abs(maxRoll);
                }
                else
                {
                    maxRoll = 100;
                }

                double roll = Math.Ceiling(Utils.GetRandomDouble() * maxRoll);

                string rollString = $"{sMsg.Author.Mention} :game_die: {roll:F0} :game_die:";

                if (double.IsInfinity(roll))
                    rollString = ":face_with_raised_eyebrow: u trying to kill me?";

                sMsg.Channel.SendMessageAsync(rollString);
            }, ".roll");
        }
    }
}
