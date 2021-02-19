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
    //i dont even wanna touch this
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

        class CallObject
        {
            public ulong Callee;
            public ulong Receiver;

            public bool CallAccepted = false;
        }

        private List<CallObject> activeCalls = new List<CallObject>();

        public MiscModule()
        {
            Commands.Add(new Command("Convert decimal number to binary", (sMsg, buffer) => {
                string binary = Convert.ToString(int.Parse(buffer.GetRemaining()), 2);
                sMsg.Channel.SendMessageAsync($"**{binary}**");

            }, ">d", ">decimaltobinary", ">dtb"));

            Commands.Add(new Command("Convert binary number to decimal", (sMsg, buffer) => {
                int val = Convert.ToInt32(buffer.GetRemaining(), 2);
                sMsg.Channel.SendMessageAsync($"**{val}**");

            }, ">b", ">binarytodecimal", ">btd"));

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

            }, ">char", ">chars", ">string"));

            Commands.Add(new Command("Convert hex to decimal", (sMsg, buffer) => {
                int val = Convert.ToInt32(buffer.GetRemaining(), 16);//int.Parse(buffer.GetRemaining(), NumberStyles.HexNumber | NumberStyles.AllowHexSpecifier);
                sMsg.Channel.SendMessageAsync($"**{val}**");

            }, ">h", ">hex"));

            Commands.Add(new Command("Make the bot say something", (sMsg, buffer) => {
                //bool delete = buffer.HasParameter("-d");

                string msg = sMsg.Content.Remove(0, 4);
                if (msg.Contains("@everyone"))
                    sMsg.Channel.SendMessageAsync($"no");
                else
                    sMsg.Channel.SendMessageAsync($"**{msg}**");
            }, ">say"));

            Commands.Add(new Command("Call another server lol", async (sMsg, buffer) =>
            {
                string message = buffer.GetRemaining().Replace("_", " ");

                CallObject activeCall1 = activeCalls.Find((o) => o.Callee == sMsg.Channel.Id);
                CallObject activeCall2 = activeCalls.Find((o) => o.Receiver == sMsg.Channel.Id);
                if (activeCall1 == null && activeCall2 == null)
                {
                    EmbedBuilder builder = new EmbedBuilder();
                    SocketGuild guildToCall = Program.Client.Guilds.ElementAt(Utils.GetRandomNumber(0, Program.Client.Guilds.Count - 1));
                    builder.WithAuthor($"{sMsg.Author.Username} Is calling from {Program.Client.GetGuild(sMsg).Name}", $"{sMsg.Author.GetAvatarUrl()}");
                    builder.WithThumbnailUrl($"{Program.Client.GetGuild(sMsg).IconUrl}");
                    builder.Description = $"With the following message: **{message}**\nDo you want to pick up??";
                    try
                    {
                        await sMsg.Channel.SendMessageAsync("Found a server! Waiting for someone to respond...");

                        var msgSend = await guildToCall.SystemChannel.SendMessageAsync("", false, builder.Build());

                        activeCalls.Add(new CallObject() { Callee = sMsg.Channel.Id, Receiver = msgSend.Channel.Id });

                        await msgSend.AddReactionsAsync(new IEmote[] { new CallAcceptEmote(), new CallDenyEmote() });
                    }
                    catch { await sMsg.Channel.SendMessageAsync("Error lol sad"); }
                }
                else
                {
                    await sMsg.Channel.SendMessageAsync("A call is already active");
                }
            }, ">call") { IsEnabled = false});

            Program.Client.ReactionAdded += async (s, e, x) =>
            {
                if (!x.User.Value.IsBot)
                {
                    var msg = await s.GetOrDownloadAsync();
                    CallObject activeCall = activeCalls.Find((o) => o.Receiver == msg.Channel.Id);
                    if (activeCall != null)
                    {
                        if (x.Emote.Name == new CallAcceptEmote().Name)
                        {
                            activeCall.CallAccepted = true;
                            await (Program.Client.GetChannel(activeCall.Receiver) as IMessageChannel).SendMessageAsync("You have accepted the call now talk!");
                            await (Program.Client.GetChannel(activeCall.Callee) as IMessageChannel).SendMessageAsync("The other party has accepted!");
                        }
                        else
                        {
                            activeCalls.Remove(activeCall);
                            await (Program.Client.GetChannel(activeCall.Receiver) as IMessageChannel).SendMessageAsync("You have closed the call!");
                            await (Program.Client.GetChannel(activeCall.Callee) as IMessageChannel).SendMessageAsync("The other party closed the call!");
                        }
                    }
                    //return Task.Delay(0);
                }
            };

            Program.Client.MessageReceived += (s) =>
            {
                if (s.Author.IsBot)
                    return Task.Delay(0);

                CallObject toSend = activeCalls.Find((o) => o.Receiver == s.Channel.Id);
                CallObject toReceive = activeCalls.Find((o) => o.Callee == s.Channel.Id);

                if (toSend != null)
                {
                    if(toSend.CallAccepted)
                    (Program.Client.GetChannel(toSend.Callee) as IMessageChannel).SendMessageAsync($":speech_balloon: **{s.Content}**");
                }

                if(toReceive != null)
                {
                    if (toReceive.CallAccepted)
                        (Program.Client.GetChannel(toReceive.Receiver) as IMessageChannel).SendMessageAsync($":speech_balloon: **{s.Content}**");
                }
                return Task.Delay(0);
            };
        }
    }
}
