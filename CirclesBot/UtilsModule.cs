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
            public ulong Caller;
            public ulong Receiver;

            public bool CallAccepted = false;
        }

        private List<CallObject> activeCalls = new List<CallObject>();

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

            AddCMD("Make the bot say something", (sMsg, buffer) =>
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

            }, ".say");

            AddCMD("Roll a random number", (sMsg, buffer) =>
            {
                double maxRoll = 100;

                if(double.TryParse(buffer.TakeFirst(), out maxRoll)) {
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

            Commands.Add(new Command("Call another server lol", async (sMsg, buffer) =>
            {
                string message = buffer.GetRemaining().Replace("_", " ");

                CallObject activeCall1 = activeCalls.Find((o) => o.Caller == sMsg.Channel.Id);
                CallObject activeCall2 = activeCalls.Find((o) => o.Receiver == sMsg.Channel.Id);
                if (activeCall1 == null && activeCall2 == null)
                {
                    EmbedBuilder builder = new EmbedBuilder();

                    SocketGuild guildToCall = CoreModule.Client.Guilds.ElementAt(Utils.GetRandomNumber(0, CoreModule.Client.Guilds.Count - 1));

                    builder.WithAuthor($"{sMsg.Author.Username} Is calling from {CoreModule.Client.GetGuild(sMsg).Name}", $"{sMsg.Author.GetAvatarUrl()}");
                    builder.WithThumbnailUrl($"{CoreModule.Client.GetGuild(sMsg).IconUrl}");
                    builder.Description = $"With the following message: **{message}**\nDo you want to pick up??";
                    try
                    {
                        var msgSendChannel = guildToCall.TextChannels.First((o) => o.Name.ToLower().StartsWith("general"));

                        await sMsg.Channel.SendMessageAsync($"Found **{msgSendChannel.Guild.Name}**! Waiting for some kind soul to respond...");

                        var msgSend = await msgSendChannel.SendMessageAsync("", false, builder.Build());

                        activeCalls.Add(new CallObject() { Caller = sMsg.Channel.Id, Receiver = msgSend.Channel.Id });

                        await msgSend.AddReactionsAsync(new IEmote[] { new CallAcceptEmote(), new CallDenyEmote() });
                    }
                    catch { await sMsg.Channel.SendMessageAsync("Error lol sad"); }
                }
                else
                {
                    await sMsg.Channel.SendMessageAsync("A call is already active");
                }
            }, ".call")
            { IsEnabled = false });

            CoreModule.Client.ReactionAdded += async (s, e, x) =>
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
                            await (CoreModule.Client.GetChannel(activeCall.Receiver) as IMessageChannel).SendMessageAsync("You have accepted the call now talk!");
                            await (CoreModule.Client.GetChannel(activeCall.Caller) as IMessageChannel).SendMessageAsync("The other party has accepted!");
                        }
                        else if (x.Emote.Name == new CallDenyEmote().Name)
                        {
                            activeCalls.Remove(activeCall);
                            await (CoreModule.Client.GetChannel(activeCall.Receiver) as IMessageChannel).SendMessageAsync("You have closed the call!");
                            await (CoreModule.Client.GetChannel(activeCall.Caller) as IMessageChannel).SendMessageAsync("The other party closed the call!");
                        }
                    }
                }
            };

            CoreModule.OnMessageReceived += (s) =>
            {
                CallObject toSend = activeCalls.Find((o) => o.Receiver == s.Channel.Id);
                CallObject toReceive = activeCalls.Find((o) => o.Caller == s.Channel.Id);

                if (toSend != null)
                {
                    if (toSend.CallAccepted)
                        (CoreModule.Client.GetChannel(toSend.Caller) as IMessageChannel).SendMessageAsync($"{s.Author.Username} :speech_balloon: **{s.Content}**");
                }

                if (toReceive != null)
                {
                    if (toReceive.CallAccepted)
                        (CoreModule.Client.GetChannel(toReceive.Receiver) as IMessageChannel).SendMessageAsync($"{s.Author.Username} :speech_balloon: **{s.Content}**");
                }
            };
        }
    }
}
