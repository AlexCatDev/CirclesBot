using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CirclesBot
{
    public class Command
    {
        public string Description { get; private set; }
        public List<string> Triggers { get; private set; }

        private Action<SocketUserMessage, CommandBuffer> onActivate;

        public bool IsEnabled { get; set; } = true;
        public double Cooldown { get; set; }

        public Command(string description, Action<SocketUserMessage, CommandBuffer> onActivate, params string[] triggers)
        {
            Description = description;
            Triggers = triggers.ToList();

            this.onActivate = onActivate;
        }

        public void Handle(SocketUserMessage userMsg)
        {
            string fullMessage = userMsg.Content.ToLower();

            List<string> args = fullMessage.Split(' ').ToList();

            string trigger = args[0];

            var cmd = Triggers.Find(triggerInList => triggerInList == trigger);
            if (cmd != null)
            {
                args.RemoveAt(0);
                if (IsEnabled)
                {
                    if (Cooldown > 0)
                    {
                        var time = DateTime.Now - Program.GetModule<SocialModule>().GetProfile(userMsg.Author.Id).LastCommand;

                        if (time.TotalSeconds < Cooldown)
                        {
                            userMsg.Channel.SendMessageAsync($"You're on cooldown: **{(Cooldown - time.TotalSeconds):F2}s**");
                            return;
                        }

                        Program.GetModule<SocialModule>().ModifyProfile(userMsg.Author.Id, profile =>
                        {
                            profile.LastCommand = DateTime.Now;
                        });
                    }
                    onActivate?.Invoke(userMsg, new CommandBuffer(args, trigger));
                    Program.TotalCommandsHandled++;
                }
                else
                {
                    userMsg.Channel.SendMessageAsync("This command has been disabled by the bot owner");
                }
            }
        }
    }

    public abstract class Module
    {
        public List<Command> Commands = new List<Command>();

        public void AddCMD(string description, Action<SocketMessage, CommandBuffer> onCommand, params string[] triggers)
        {
            Commands.Add(new Command(description, onCommand, triggers));
        }

        public abstract string Name { get; }
        public abstract int Order { get; }
    }
}
