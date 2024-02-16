using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;

namespace Bot
{
    internal static class Player
    {
        public async static void StartPlay(DiscordClient client, DiscordMember member, string number)
        {

            LavalinkExtension lavalink = client.GetLavalink();
            LavalinkNodeConnection node = null;
            LavalinkGuildConnection connection = null;
            if (lavalink.ConnectedNodes.Count > 0)
                node = lavalink.ConnectedNodes.First().Value;

            DiscordChannel channel = member.VoiceState.Channel;

            if (node != null)
                if (channel.Type == ChannelType.Voice)
                    if(!node.ConnectedGuilds.ContainsKey(channel.Guild.Id))
                        connection = await node.ConnectAsync(channel);
                    else
                        connection = node.ConnectedGuilds[channel.Guild.Id];

            Dictionary<string, Dictionary<string, string>> RadioStreamsDictionary = MessageFormat.TakeRadioStreams();
            var RadioStream = RadioStreamsDictionary[number].Values.First();
            LavalinkLoadResult loadResult = await node.Rest.GetTracksAsync(RadioStream, LavalinkSearchType.Plain);
            if (loadResult.Tracks.Count() > 0)
            {
                LavalinkTrack track = loadResult.Tracks.First();
                await connection.PlayAsync(track);

                Program.connController.WaitOne();
                InitStopTimer(connection, number);
                Program.connController.Set();
            }
            else
            {
                await connection.DisconnectAsync();
                DeleteStopTimer(connection);
                DiscordChannel botChannel = null;
                foreach(var chan in channel.Guild.Channels.Values)
                {
                    if (chan.Name == "radio_by_yagor")
                    {
                        botChannel = chan;
                        break;
                    }
                }
                await botChannel.SendMessageAsync("Станция временно не работает");

            }

        }
        static void InitStopTimer(LavalinkGuildConnection connection, string number)
        {
            if (!Program.guildConnections.ContainsKey(connection))
                Program.guildConnections.Add(connection, new KeyValuePair<byte, string>(0, number));
        }
        public static void DeleteStopTimer(LavalinkGuildConnection connection)
        {
            if(!Program.guildConnections.ContainsKey(connection))
                Program.guildConnections.Remove(connection);
        }
    }
}
