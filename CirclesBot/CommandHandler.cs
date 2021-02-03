using Discord.WebSocket;
using System.Collections.Generic;
using System.Linq;

namespace CirclesBot
{
    public static class CommandHandler
    {
        public class Command
        {
            public List<string> Triggers;
            public string Description;
            public CommandCallBack CommandCallBack;

            public bool IsActive => CommandCallBack != null;
        }

        public delegate void CommandCallBack(SocketMessage socketMsg, CommandBuffer buffer);

        private static List<Command> commands = new List<Command>();

        public static int ActiveCommands => commands.Count;

        public static IReadOnlyList<Command> Commands => commands.AsReadOnly();

        public static void AddCommand(string description, CommandCallBack cmdCallback, params string[] triggers)
        {
            var command = new Command() { Triggers = triggers.ToList(), Description = description, CommandCallBack = cmdCallback };

            commands.Add(command);
        }

        public static void Handle(SocketMessage msg)
        {
            string fullMessage = msg.Content.ToLower();

            List<string> args = fullMessage.Split(' ').ToList();

            string trigger = args[0];

            var cmd = commands.Find(o => o.Triggers.Contains(trigger));
            if (cmd != null)
            {
                args.RemoveAt(0);
                cmd?.CommandCallBack(msg, new CommandBuffer(args));
            }
        }
    }
}
