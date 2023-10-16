using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using DSharpPlus.Net;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Logging;
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
            AutoResetEvent controller = new AutoResetEvent(true);
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
                        await Player.StartPlay(client, invoker, number);
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
                        if (guildConnections.ContainsKey(connection))
                            guildConnections.Remove(connection);

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

                        userCount--;

                        await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Поток остановлен"));

                    }
                }
            };
            client.GuildCreated += (s, e) =>
            {
                Task.Run(() =>
                {
                    controller.WaitOne();
                    MessageFormat.UpdateList(e.Guild).GetAwaiter().GetResult();
                    controller.Set();
                });
                return Task.CompletedTask;
            };

            LavalinkExtension lavalink = client.UseLavalink();
            lavalink.NodeDisconnected += async (s, e) => await lavalink.ConnectAsync(lavalinkConfig);
            SlashCommandsExtension cmdExt = client.UseSlashCommands();
            cmdExt.RegisterCommands<Commands>();
            await client.ConnectAsync();
            await lavalink.ConnectAsync(lavalinkConfig);

            System.Timers.Timer stopTimer = new System.Timers.Timer(new TimeSpan(0, 10, 0).TotalMilliseconds);
            System.Timers.Timer updateTimer = new System.Timers.Timer(new TimeSpan(0, 15, 0).TotalMilliseconds); //MessageFormat.UpdateList раз в 15 минут

            stopTimer.Elapsed += async (s, e) =>
            {
                try
                {
                    var connections = lavalink.ConnectedNodes.First().Value.ConnectedGuilds.Values.ToArray();
                    for (int i = 0; i < connections.Length; i++)
                    {
                        LavalinkGuildConnection connection = connections[i];
                        byte counter;
                        if (connection.IsConnected && guildConnections.ContainsKey(connection))
                        {
                            counter = guildConnections[connection];
                            DiscordChannel channel = connection.Channel;
                            if (channel.Users.Count == 1 && counter == 1)
                            {
                                if (connection.IsConnected)
                                    await connection.DisconnectAsync();
                                guildConnections.Remove(connection);
                                Program.userCount--;
                            }
                            else if (channel.Users.Count == 1 && counter == 0)
                            {
                                guildConnections.Remove(connection);
                                guildConnections.Add(connection, ++counter);
                            }
                            else if (channel.Users.Count > 1 && counter != 0)
                            {
                                guildConnections.Remove(connection);
                                guildConnections.Add(connection, --counter);
                            }
                        }
                        else if (!connection.IsConnected && guildConnections.ContainsKey(connection))
                        {
                            guildConnections.Remove(connection);
                        }
                        else if (connection.IsConnected && !guildConnections.ContainsKey(connection))
                        {
                            guildConnections.Add(connection, 0);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(ex.Message);
                    Console.ForegroundColor = ConsoleColor.White;
                }
            };
            updateTimer.Elapsed += async (s, e) =>
            {
                foreach (DiscordGuild guild in client.Guilds.Values)
                {
                    controller.WaitOne();
                    await MessageFormat.UpdateList(guild);
                    controller.Set();
                }
            };
            stopTimer.Start();
            updateTimer.Start();
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

            config = new DiscordConfiguration
            {
                TokenType = TokenType.Bot,
                MinimumLogLevel = LogLevel.Information,
                Intents = DiscordIntents.AllUnprivileged,
                AutoReconnect = true
            };

            using (StreamReader reader = new StreamReader("Token.txt"))
            {
                config.Token = reader.ReadLine();
            }

            client = new DiscordClient(config);
            number = Int32.Parse(MessageFormat.createList(MessageFormat.TakeRadioStreams()).Substring(0, 1));

            userCount = 0;
            guildConnections = new Dictionary<LavalinkGuildConnection, byte>();
        }
        public static DiscordClient client;
        static DiscordConfiguration config;
        static LavalinkConfiguration lavalinkConfig;
        static ConnectionEndpoint endpoint;
        public static int number;//номер первой станции
        internal static int userCount;
        internal static Dictionary<LavalinkGuildConnection, byte> guildConnections;
    }
}
