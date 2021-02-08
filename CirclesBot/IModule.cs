using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CirclesBot
{
    public interface IModule
    {
        public string Name { get; }

        public void OnMessageReceived(DiscordSocketClient client, SocketUserMessage message);

        public void OnReactionAdded(DiscordSocketClient client, SocketUserMessage message);

        public void OnReactionRemoved(DiscordSocketClient client, SocketUserMessage message);

        public void OnMessageEdited(DiscordSocketClient client, SocketUserMessage message);
    }
}
