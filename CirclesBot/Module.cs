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
                onActivate?.Invoke(userMsg, new CommandBuffer(args));
            }
        }
    }

    public abstract class Module
    {
        public List<Command> Commands = new List<Command>();

        public abstract string Name { get; }
    }
}
