using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CirclesBot
{
    public class MiscModule : Module
    {
        public override string Name => "Misc Module";

        class CallAcceptEmote : IEmote
        {
            public string Name => "✅";
        }

        class CallDenyEmote : IEmote
        {
            public string Name => "❌";
        }

        public MiscModule()
        {
            Commands.Add(new Command("Convert decimal number to binary", (sMsg, buffer) => {
                string binary = Convert.ToString(int.Parse(buffer.GetRemaining()), 2);
                sMsg.Channel.SendMessageAsync($"**{binary}**");

            }, ">d", ">decimal"));

            Commands.Add(new Command("Convert binary number to decimal", (sMsg, buffer) => {
                int val = Convert.ToInt32(buffer.GetRemaining(), 2);
                sMsg.Channel.SendMessageAsync($"**{val}**");

            }, ">b", ">binary"));

            Commands.Add(new Command("Convert binary to chars", (sMsg, buffer) => {
                var list = new System.Collections.Generic.List<Byte>();
                string binary = buffer.GetRemaining();

                binary = binary.Replace("_", "");

                for (int i = 0; i < binary.Length; i += 8)
                {
                    try
                    {
                        String t = binary.Substring(i, 8);

                        list.Add(Convert.ToByte(t, 2));
                    }
                    catch { }
                }

                string output = System.Text.Encoding.ASCII.GetString(list.ToArray());

                if (output.Contains("@everyone"))
                    sMsg.Channel.SendMessageAsync($"no");
                else
                    sMsg.Channel.SendMessageAsync($"**{output}**");

            }, ">charstobin", ">cbinary", ">char", ">chars", ">binarytostring", ">cb", ">string"));

            Commands.Add(new Command("Convert hex to decimal", (sMsg, buffer) => {
                int val = Convert.ToInt32(buffer.GetRemaining(), 16);//int.Parse(buffer.GetRemaining(), NumberStyles.HexNumber | NumberStyles.AllowHexSpecifier);
                sMsg.Channel.SendMessageAsync($"**{val}**");

            }, ">h", ">hex"));

            Commands.Add(new Command("Make the bot say whatever", async (sMsg, buffer) => {
                //bool delete = buffer.HasParameter("-d");

                string msg = sMsg.Content.Remove(0, 4);
                if (msg.Contains("@everyone"))
                    sMsg.Channel.SendMessageAsync($"no");
                else
                    sMsg.Channel.SendMessageAsync($"**{msg}**");
            }, ">say"));

            Commands.Add(new Command("ooga booga", async (sMsg, buffer) => {
                EmbedBuilder builder = new EmbedBuilder();
                builder.WithAuthor($"{sMsg.Author.Username} Is calling from {Program.Client.GetGuild(sMsg).Name}", $"{sMsg.Author.GetAvatarUrl()}");
                builder.WithThumbnailUrl($"{Program.Client.GetGuild(sMsg).IconUrl}");
                builder.Description = "Do you want to pick up??";

                var msgSend = await sMsg.Channel.SendMessageAsync("", false, builder.Build());
                await msgSend.AddReactionsAsync(new IEmote[] { new CallAcceptEmote(), new CallDenyEmote() });
            }, ">call"));
        }
    }
}
