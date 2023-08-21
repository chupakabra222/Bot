using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using DSharpPlus.SlashCommands;
using System.Text;

namespace Bot
{
    class Commands : ApplicationCommandModule
    {
        [SlashCommand("play", "play radio stream")]
        public async Task PlayCommand(InteractionContext ctx, [Option("RadioStream", $"Введите номер радиостанции")] string number)
        {
            if (ctx.Member.VoiceState == null)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Вы не подключенны к какому-либо голосовому каналу"));
                return;
            }
            DiscordChannel channel = ctx.Member.VoiceState.Channel;
            LavalinkExtension lavalink = ctx.Client.GetLavalink();

            if (!lavalink.ConnectedNodes.Any())
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Проблема на стороне сервера"));
                return;
            }
            await Player.StartPlay(ctx.Client, ctx.Member, number);
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Начато транслирование"));
        }
        [SlashCommand("List", "It sends a list of radio stations for your choice")]
        public async Task GetRadioStations(InteractionContext ctx)
        {
            var RadioStreams = MessageFormat.TakeRadioStreams();
            string list = MessageFormat.createList(RadioStreams);
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Список радиостанций :"));
            string adminMsg = "";
            using (StreamReader reader = new StreamReader("message.txt"))
            {
                adminMsg = reader.ReadToEnd();
            }
            while (list.Length > 2000)
            {
                string[] strs = list.Split('\r');
                StringBuilder builder = new StringBuilder();

                for (int i = 0; i < 70; i++)
                {
                    builder.Append(strs[i]);
                    builder.Append("\r");
                }

                await ctx.Channel.SendMessageAsync(new DiscordMessageBuilder().WithContent(builder.ToString()));

                builder = new StringBuilder();

                for (int i = 70; i < strs.Length; i++)
                {
                    builder.Append(strs[i]);
                    builder.Append("\r");
                }

                list = builder.ToString();
            }
            DiscordSelectComponent selectMenu = MessageFormat.CreateSelectMenu(1, 20);
            DiscordButtonComponent button = new DiscordButtonComponent(ButtonStyle.Secondary, "Stop", "", false, new DiscordComponentEmoji(DiscordEmoji.FromName(ctx.Client, ":stop_button:")));
            await ctx.Channel.SendMessageAsync(new DiscordMessageBuilder().WithContent(list).AddComponents(selectMenu).AddComponents(button));
            await ctx.Channel.SendMessageAsync(new DiscordMessageBuilder().WithContent(adminMsg));

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

            await connection.DisconnectAsync();
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Поток остановлен"));
        }
    }
}
