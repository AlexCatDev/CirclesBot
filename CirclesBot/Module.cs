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
        /// <summary>
        /// The text that shows an example of how to use the command, or something else helpful/hinting
        /// </summary>
        public string HelpText { get; private set; }
        /// <summary>
        /// What the command does
        /// </summary>
        public string Description { get; private set; }
        /// <summary>
        /// What text triggers this command, needs to include the prefix, like !help
        /// </summary>
        public List<string> Triggers { get; private set; }

        private Action<SocketUserMessage, CommandBuffer> onActivate;

        /// <summary>
        /// Is this command enabled? (Default: True)
        /// </summary>
        public bool IsEnabled { get; set; } = true;
        /// <summary>
        /// The cooldown of this command in seconds
        /// </summary>
        public double Cooldown { get; set; }

        private Dictionary<ulong, DateTime> cooldownDictionary = new Dictionary<ulong, DateTime>();

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
            if (string.IsNullOrEmpty(cmd) == false)
            {
                args.RemoveAt(0);

                if (!IsEnabled)
                {
                    userMsg.Channel.SendMessageAsync("This command has been disabled for some reason :thinking:");
                    return;
                }

                if (Cooldown > 0)
                {
                    if (cooldownDictionary.TryGetValue(userMsg.Author.Id, out var lastInvokeTime))
                    {
                        var timeDiff = DateTime.Now - lastInvokeTime;
                        if (timeDiff.TotalSeconds < Cooldown)
                        {
                            userMsg.Channel.SendMessageAsync($"You are on cooldown! **{(Cooldown - timeDiff.TotalSeconds):F2}s** left");
                            return;
                        }
                        else
                            cooldownDictionary[userMsg.Author.Id] = DateTime.Now;
                    }
                    else
                        cooldownDictionary.Add(userMsg.Author.Id, DateTime.Now);
                }
                onActivate?.Invoke(userMsg, new CommandBuffer(args, trigger));
                CoreModule.TotalCommandsHandled++;
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
