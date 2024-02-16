using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using DSharpPlus.SlashCommands;
using System.Text;

namespace Bot
{
    class Commands : ApplicationCommandModule
    {
        [SlashCommand("Play", "Play radio stream or video")]
        public async Task PlayCommand(InteractionContext ctx, [Option("RadioStream", $"Введите номер радиостанции")] string number)
        {
            if (ctx.Member.VoiceState == null)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Вы не подключенны к какому-либо голосовому каналу"));
                return;
            }
            LavalinkExtension lavalink = ctx.Client.GetLavalink();

            if (!lavalink.ConnectedNodes.Any())
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Проблема на стороне сервера"));
                return;
            }
            Player.StartPlay(ctx.Client, ctx.Member, number);
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Начато транслирование"));
        }
        [SlashCommand("List", "It sends a list of radio stations for your choice")]
        public async Task GetRadioStations(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Список радиостанций :"));
            await MessageFormat.SendList(ctx.Channel);
        }
        [SlashCommand("Stop", "Stop stream")]
        public async Task Stop(InteractionContext ctx)
        {
            LavalinkExtension lavalink = ctx.Client.GetLavalink();
            if (ctx.Member.VoiceState == null)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Вы не подключенны к какому-либо голосовому каналу"));
                return;
            }
            if (!lavalink.ConnectedNodes.Any())
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("The Lavalink connection is not established"));
                return;
            }
            LavalinkNodeConnection node = lavalink.ConnectedNodes.Values.First();

            LavalinkGuildConnection connection = node.GetGuildConnection(ctx.Guild);
            if (connection == null)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Bot is not connected to any voice channel"));
                return;
            }

            Program.connController.WaitOne();
            Player.DeleteStopTimer(connection);
            Program.connController.Set();

            await connection.DisconnectAsync();
            

            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Поток остановлен"));
        }
    }
}
