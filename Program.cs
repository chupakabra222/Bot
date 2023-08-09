using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using DSharpPlus.Net;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Data;
using System.Text;
using System.IO;

namespace Bot
{
    static class Program
    {
        static void Main(string[] args)
        {
            MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();
        }
        static async Task MainAsync(string[] args)
        {

            DiscordConfiguration config = new DiscordConfiguration
            {
                TokenType = TokenType.Bot,
                MinimumLogLevel = LogLevel.Debug,
                Intents = DiscordIntents.AllUnprivileged,
                AutoReconnect = true
            };
            using(StreamReader reader = new StreamReader("Token.txt"))
            {
                config.Token = reader.ReadLine();
            }
            client = new DiscordClient(config);
            client.GuildCreated += async (s, e) =>
            {
                await MessageFormat.UpdateList(e.Guild);
                StartUpdateTask(e.Guild);
            };
            client.GuildAvailable += async (s, e) =>
            {
                await MessageFormat.UpdateList(e.Guild);
                StartUpdateTask(e.Guild);
            };
            client.GuildDeleted += (s, e) =>
            {
                CancTokens[e.Guild.Id].Cancel();
                return Task.CompletedTask;
            };
            client.ComponentInteractionCreated += async (s, e) =>
            {
                int ind;
                int count = MessageFormat.TakeRadioStreams().Count;
                string list = e.Message.Content;
                DiscordSelectComponent selectMenu;
                DiscordSelectComponent oldSelectMenu = e.Message.Components.First().Components.First() as DiscordSelectComponent;
                DiscordButtonComponent button = e.Message.Components.Last().Components.First() as DiscordButtonComponent;
                DiscordMember invoker = await e.Guild.GetMemberAsync(e.User.Id);
                IEnumerable<DiscordSelectComponentOption> RadioList;
                if (e.Interaction.Data.Values.Length > 0)
                    if (e.Interaction.Data.Values.First() == "Next")
                    {
                        RadioList = MessageFormat.GetExceptList(true, oldSelectMenu.Options);
                        Int32.TryParse(RadioList.Last().Value, out ind);
                        if (count - ind > 20)
                            selectMenu = MessageFormat.CreateSelectMenu(ind + 1, ind + 20);

                        else
                            selectMenu = MessageFormat.CreateSelectMenu(ind + 1, count);

                        await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().WithContent(list).AddComponents(selectMenu).AddComponents(button));
                    }
                    else if (e.Interaction.Data.Values.First() == "Back")
                    {
                        RadioList = MessageFormat.GetExceptList(false, oldSelectMenu.Options);
                        Int32.TryParse(RadioList.First().Value, out ind);
                        if (ind > 20)
                        {
                            selectMenu = MessageFormat.CreateSelectMenu(ind - 20, ind - 1);
                        }
                        else
                            selectMenu = MessageFormat.CreateSelectMenu(Program.number, ind - 1);
                        await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().WithContent(list).AddComponents(selectMenu).AddComponents(button));
                    }
                    else
                    {
                        string number = e.Interaction.Data.Values.First();
                        if (invoker.VoiceState == null)
                        {
                            await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Вы не подключены к какому-либо голосовому каналу"));
                            return;
                        }
                        DiscordChannel channel = invoker.VoiceState.Channel;
                        MessageFormat.StartPlay(client, invoker, number);
                        await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().WithContent(list).AddComponents(oldSelectMenu).AddComponents(button));
                    }
                else
                {
                    if (e.Interaction.Data.CustomId == "Stop")
                    {
                        LavalinkExtension lavalink = client.GetLavalink();
                        if (!lavalink.ConnectedNodes.Any())
                        {
                            await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Проблема на стороне сервера"));
                            return;
                        }
                        LavalinkNodeConnection node = lavalink.ConnectedNodes.First().Value;
                        LavalinkGuildConnection connection = node.GetGuildConnection(e.Interaction.Guild);
                        if (connection == null)
                        {
                            await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Бот не подключен к какому-либо голосовому каналу"));
                            return;
                        }
                        if (invoker.VoiceState == null)
                        {
                            await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Вы не подключены к какому-либо голосовому каналу"));
                            return;
                        }
                        await connection.DisconnectAsync();
                        await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Поток остановлен"));

                    }
                }
            };

            LavalinkExtension lavalink = client.UseLavalink();
            lavalink.NodeDisconnected += async (s, e) => await lavalink.ConnectAsync(lavalinkConfig);

            SlashCommandsExtension cmdExt = client.UseSlashCommands();
            cmdExt.RegisterCommands<Commands>();
            await client.ConnectAsync();
            await lavalink.ConnectAsync(lavalinkConfig);


            System.Timers.Timer statTimer = new System.Timers.Timer(900000);
            statTimer.Elapsed += (s, e) => { GetStaticstics(client); };
            statTimer.Start();


            await Task.Delay(-1);
        }
        static Program()
        {
            endpoint = new ConnectionEndpoint
            {
                Hostname = "127.0.0.1",
                Port = 2333
            };
            lavalinkConfig = new LavalinkConfiguration
            {
                Password = "youshallnotpass",
                RestEndpoint = endpoint,
                SocketEndpoint = endpoint
            };
            number = Int32.Parse(MessageFormat.createList(MessageFormat.TakeRadioStreams()).Substring(0, 1));
            CancTokens = new Dictionary<ulong, CancellationTokenSource>();
        }
        static ConnectionEndpoint endpoint;
        static LavalinkConfiguration lavalinkConfig;
        public static int number;
        public static DiscordClient client;
        static Dictionary<ulong, CancellationTokenSource> CancTokens;
        static void GetStaticstics(object cl)
        {
            Console.WriteLine(Thread.CurrentThread.ManagedThreadId);
            DiscordClient client = (DiscordClient)cl;
            using (StreamWriter writer = new StreamWriter("Statistics.txt", Encoding.Unicode, new FileStreamOptions()
            {
                Mode = FileMode.OpenOrCreate,
                Access = FileAccess.Write,
                Share = FileShare.ReadWrite
            }))
            {
                StringBuilder builder = new StringBuilder();
                while (client.Guilds.Count == 0) { }
                foreach (DiscordGuild current in client.Guilds.Values)
                {
                    Thread.Sleep(5000);
                    builder.Append("Server name :");
                    builder.Append("    ");
                    builder.Append(current.Name);
                    builder.Append("    ");
                    builder.Append("Server Id");
                    builder.Append("    ");
                    builder.Append(current.Id);
                    builder.AppendLine();
                }
                writer.Write(builder.ToString());
                writer.WriteLine($"Count : {client.Guilds.Count}");
                Console.WriteLine(builder.ToString());
            }
        }
        static void StartUpdateTask(DiscordGuild guild)
        {
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            ulong guildId = guild.Id;
            CancTokens.Add(guildId, tokenSource);

            Task.Run(() =>
            {
                System.Timers.Timer timer = new System.Timers.Timer(900000);//15 minutes
                timer.Elapsed += async (s, e) =>
                {
                    try
                    {
                        if (CancTokens[guildId].Token.IsCancellationRequested)
                            CancTokens[guildId].Token.ThrowIfCancellationRequested();

                        await MessageFormat.UpdateList(guild);
                    }
                    catch (OperationCanceledException ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(ex.Message);
                        Console.ForegroundColor = ConsoleColor.White;
                        CancTokens.Remove(guildId);
                        timer.Dispose();
                    }
                };
                timer.Start();
            }, tokenSource.Token);

        }
    }
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
            await MessageFormat.StartPlay(ctx.Client, ctx.Member, number);
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Начато транслирование"));
        }
        [SlashCommand("List", "It sends a list of radio stations for your choice")]
        public async Task GetRadioStations(InteractionContext ctx)
        {
            var RadioStreams = MessageFormat.TakeRadioStreams();
            string list = MessageFormat.createList(RadioStreams);
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Список радиостанций :"));
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
    static class MessageFormat
    {
        public static string createList(Dictionary<string, Dictionary<string, string>> RadioStreams)
        {
            StringBuilder builder = new StringBuilder();
            foreach (var radioStream in RadioStreams)
            {
                var number = radioStream.Key;
                var name = radioStream.Value.Keys.First();
                builder.Append(number);
                builder.Append(" ");
                builder.Append(name);
                builder.Append("\r");
            }
            return builder.ToString().Trim();
        }
        public static Dictionary<string, Dictionary<string, string>> TakeRadioStreams()
        {
            string json = "";
            Dictionary<string, Dictionary<string, string>> values = null;
            using (StreamReader reader = new StreamReader("RadioStreams/RadioStream.json", new FileStreamOptions { Mode = FileMode.Open, Access = FileAccess.Read, Share = FileShare.ReadWrite }))
            {
                while (json.Length == 0)
                    json = reader.ReadToEnd();
            }
            if (json != null)
                values = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(json);
            if (values == null)
            {
                Console.WriteLine(2);
            }
            return values;
        }
        public static DiscordSelectComponent CreateSelectMenu(int start, int end)
        {
            var options = new List<DiscordSelectComponentOption>();
            var RadioStreams = TakeRadioStreams();

            if (start > Program.number)
                options.Add(new DiscordSelectComponentOption("Вверх", "Back"));

            for (; start <= end; start++)
            {
                string name, key = "";
            Name:
                try
                {
                    name = $"{start} {RadioStreams[start.ToString()].Keys.First()}";
                }
                catch (Exception e)
                {
                    start++;
                    end++;
                    goto Name;
                }
                options.Add(new DiscordSelectComponentOption(name, start.ToString()));
            }

            if (RadioStreams.Count != end)
                options.Add(new DiscordSelectComponentOption("Вниз", "Next"));

            var dropDown = new DiscordSelectComponent((new Random().Next()).ToString(), "Выберите радиостанцию", options);
            return dropDown;
        }
        public static IEnumerable<DiscordSelectComponentOption> GetExceptList(bool a, IEnumerable<DiscordSelectComponentOption> list)
        {
            IEnumerable<DiscordSelectComponentOption> RadioList;
            List<DiscordSelectComponentOption> exceptElem = new List<DiscordSelectComponentOption>();
            if (a)
            {
                exceptElem.Add(list.Last());
                RadioList = list.Except(exceptElem);
            }
            else
            {
                exceptElem.Add(list.First());
                RadioList = list.Except(exceptElem);
            }
            return RadioList;
        }
        public static async Task StartPlay(DiscordClient client, DiscordMember member, string number)
        {

            LavalinkExtension lavalink = client.GetLavalink();
            LavalinkNodeConnection node = lavalink.ConnectedNodes.First().Value;
            DiscordChannel channel = member.VoiceState.Channel;
            LavalinkGuildConnection connection = await node.ConnectAsync(channel);
            Dictionary<string, Dictionary<string, string>> RadioStreamsDictionary = TakeRadioStreams();
            var RadioStream = RadioStreamsDictionary[number].Values.First();
            LavalinkLoadResult loadResult = await node.Rest.GetTracksAsync(RadioStream, LavalinkSearchType.Plain);
            LavalinkTrack track = loadResult.Tracks.First();
            await connection.PlayAsync(track);
            await Task.Run(() =>
            {
                System.Timers.Timer timer = new System.Timers.Timer(600000);//10 minutes
                byte counter = 0;
                timer.Elapsed += async (s, e) =>
                {
                    if (channel.Users.Count == 1 && counter == 1)
                    {
                        await connection.DisconnectAsync();
                        timer.Dispose();
                    }
                    else if (channel.Users.Count == 1 && counter == 0)
                        counter++;
                    else if (channel.Users.Count > 1 && counter != 0)
                        counter--;
                };
                timer.Start();
            });
        }
        public async static Task UpdateList(object gl)
        {
            try
            {
                DiscordGuild guild = (DiscordGuild)gl;
                IEnumerable<DiscordChannel> channels = await guild.GetChannelsAsync();
                DiscordChannel channel;

                channels = channels.Where((ch) =>
                {
                    if (ch.Name == "radio_by_yagor")
                        return true;
                    return false;
                });

                if (channels.Any())
                    channel = channels.First();
                else
                    channel = await guild.CreateChannelAsync("radio_by_yagor", ChannelType.Text);

                IEnumerable<DiscordMessage> messages = await channel.GetMessagesAsync();
                DiscordSelectComponent selectMenu;
                DiscordButtonComponent button;


                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"update {channel.Guild.Name}");
                Console.ForegroundColor = ConsoleColor.White;



                Dictionary<string, Dictionary<string, string>> RadioStreams = null;
                while (RadioStreams == null)
                    RadioStreams = TakeRadioStreams();

                Stack<DiscordMessage> messages1 = new Stack<DiscordMessage>();
                string mess = "";
                if (messages.Any())
                {

                    var delMessages = messages.Where(m =>
                    {
                        if ((m.Author == Program.client.CurrentUser && m.Content.Length < 100) || m.Author != Program.client.CurrentUser)
                            return true;
                        return false;
                    });
                    messages = messages.Where(m =>
                    {
                        if (m.Author == Program.client.CurrentUser && m.Content.Length > 100)
                            return true;
                        return false;
                    });
                    if (delMessages.Any())
                        await channel.DeleteMessagesAsync(delMessages);

                    //контент текущих сообщений
                    foreach (var a in messages)
                        messages1.Push(a);
                    foreach (var a in messages1)
                    {
                        mess += a.Content;
                        mess += "\r";
                    }
                    mess = mess.Trim();
                }

                string b = createList(TakeRadioStreams());
                if (mess != b)
                {

                    string list = createList(RadioStreams);

                    //номер первой станции
                    string strNumber = list.Substring(0, 1);
                    Program.number = Int32.Parse(strNumber);

                    if (messages.Any())
                        await channel.DeleteMessagesAsync(messages);

                    while (list.Length > 2000)
                    {
                        string[] strs = list.Split('\r');
                        StringBuilder builder = new StringBuilder();

                        for (int i = 0; i < 70; i++)
                        {
                            builder.Append(strs[i]);
                            builder.Append("\r");
                        }

                        await channel.SendMessageAsync(new DiscordMessageBuilder().WithContent(builder.ToString()));

                        builder = new StringBuilder();

                        for (int i = 70; i < strs.Length; i++)
                        {
                            builder.Append(strs[i]);
                            builder.Append("\r");
                        }

                        list = builder.ToString();

                    }

                    selectMenu = CreateSelectMenu(Program.number, 20);
                    button = new DiscordButtonComponent(ButtonStyle.Secondary, "Stop", "", false, new DiscordComponentEmoji(DiscordEmoji.FromName(Program.client, ":stop_button:")));
                    await channel.SendMessageAsync(new DiscordMessageBuilder().WithContent(list).AddComponents(selectMenu).AddComponents(button));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}