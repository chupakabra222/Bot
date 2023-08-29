using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using DSharpPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot
{
    internal static class Player
    {
        public static async Task StartPlay(DiscordClient client, DiscordMember member, string number)
        {

            LavalinkExtension lavalink = client.GetLavalink();
            LavalinkNodeConnection node = lavalink.ConnectedNodes.First().Value;
            DiscordChannel channel = member.VoiceState.Channel;
            LavalinkGuildConnection connection = await node.ConnectAsync(channel);
            Dictionary<string, Dictionary<string, string>> RadioStreamsDictionary = MessageFormat.TakeRadioStreams();
            var RadioStream = RadioStreamsDictionary[number].Values.First();
            LavalinkLoadResult loadResult = await node.Rest.GetTracksAsync(RadioStream, LavalinkSearchType.Plain);
            LavalinkTrack track = loadResult.Tracks.First();
            await connection.PlayAsync(track);

            Program.userCount++;

            await Task.Run(() =>
            {
                System.Timers.Timer timer = new System.Timers.Timer(600000);//10 minutes
                byte counter = 0;
                timer.Elapsed += async (s, e) =>
                {
                    if (channel.Users.Count == 1 && counter == 1)
                    {
                        if (connection.IsConnected)
                            await connection.DisconnectAsync();
                        timer.Dispose();
                    }
                    else if (channel.Users.Count == 1 && counter == 0)
                        counter++;
                    else if (channel.Users.Count > 1 && counter != 0)
                        counter--;
                };
                timer.Start();
            });//отключить воспроизведение если никого нет 20 минут
        }
    }
}
