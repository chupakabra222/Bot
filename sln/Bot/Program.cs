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
            AutoResetEvent updateController = new AutoResetEvent(true);

            LavalinkExtension lavalink = client.UseLavalink();
            SlashCommandsExtension cmdExt = client.UseSlashCommands();
            cmdExt.RegisterCommands<Commands>();
            await client.ConnectAsync();
            await lavalink.ConnectAsync(lavalinkConfig);

            client.ComponentInteractionCreated += async (s, e) =>
            {
                int ind;
                int count = MessageFormat.TakeRadioStreams().Count;
                string list = e.Message.Content;
                DiscordSelectComponent selectMenu;
                DiscordSelectComponent oldSelectMenu = e.Message.Components.First().Components.First() as DiscordSelectComponent;
                DiscordButtonComponent stopBtn = e.Message.Components.Last().Components.First() as DiscordButtonComponent;
                DiscordButtonComponent backBtn = e.Message.Components.Last().Components.Skip(1).First() as DiscordButtonComponent;
                DiscordButtonComponent nextBtn = e.Message.Components.Last().Components.Skip(2).First() as DiscordButtonComponent;
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

                        await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().WithContent(list).AddComponents(selectMenu).AddComponents(stopBtn, backBtn, nextBtn));
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
                        await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().WithContent(list).AddComponents(selectMenu).AddComponents(stopBtn, backBtn, nextBtn));
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
                        Player.StartPlay(client, invoker, number);
                        await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().WithContent(list).AddComponents(oldSelectMenu).AddComponents(stopBtn, backBtn, nextBtn));
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
                        if (connection != null)
                            Player.DeleteStopTimer(connection);

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
                    else if(e.Interaction.Data.CustomId == "Next")
                    {
                        LavalinkExtension lavalink = client.GetLavalink();
                        if (!lavalink.ConnectedNodes.Any())
                        {
                            await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Проблема на стороне сервера"));
                            return;
                        }
                        LavalinkNodeConnection node = lavalink.ConnectedNodes.First().Value;

                        LavalinkGuildConnection connection = node.GetGuildConnection(e.Interaction.Guild);
                        if (invoker.VoiceState == null)
                        {
                            await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Вы не подключены к какому-либо голосовому каналу"));
                            return;
                        }

                        if (connection == null)
                        {
                            Player.StartPlay(client, invoker, "1");
                            await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().WithContent(list).AddComponents(oldSelectMenu).AddComponents(stopBtn, backBtn, nextBtn));
                            return;
                        }
                        else
                        {
                            connController.WaitOne();
                            int number = int.Parse(guildConnections[connection].Value);
                            byte control = guildConnections[connection].Key;

                            if (number < MessageFormat.TakeRadioStreams().Count)
                                number++;
                            else
                                number = 1;

                            guildConnections.Remove(connection);
                            guildConnections.Add(connection, new KeyValuePair<byte, string>(control, number.ToString()));
                            connController.Set();
                            Player.StartPlay(client, invoker, number.ToString());
                            await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().WithContent(list).AddComponents(oldSelectMenu).AddComponents(stopBtn, backBtn, nextBtn));
                        }
                    }
                    else if (e.Interaction.Data.CustomId == "Back")
                    {
                        LavalinkExtension lavalink = client.GetLavalink();
                        if (!lavalink.ConnectedNodes.Any())
                        {
                            await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Проблема на стороне сервера"));
                            return;
                        }
                        LavalinkNodeConnection node = lavalink.ConnectedNodes.First().Value;

                        LavalinkGuildConnection connection = node.GetGuildConnection(e.Interaction.Guild);
                        if (invoker.VoiceState == null)
                        {
                            await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Вы не подключены к какому-либо голосовому каналу"));
                            return;
                        }

                        if (connection == null)
                        {
                            Player.StartPlay(client, invoker, MessageFormat.TakeRadioStreams().Count.ToString());
                            await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().WithContent(list).AddComponents(oldSelectMenu).AddComponents(stopBtn, backBtn, nextBtn));
                            return;
                        }
                        else
                        {
                            connController.WaitOne();
                            int number = int.Parse(guildConnections[connection].Value);
                            byte control = guildConnections[connection].Key;

                            if (number > 0)
                                number--;
                            else
                                number = MessageFormat.TakeRadioStreams().Count;

                            guildConnections.Remove(connection);
                            guildConnections.Add(connection, new KeyValuePair<byte, string>(control, number.ToString()));
                            connController.Set();
                            Player.StartPlay(client, invoker, number.ToString());
                            await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().WithContent(list).AddComponents(oldSelectMenu).AddComponents(stopBtn, backBtn, nextBtn));
                        }
                    }
                }
            };
            client.GuildCreated += (s, e) =>
            {
                Task.Run(() =>
                {
                    updateController.WaitOne();
                    MessageFormat.UpdateList(e.Guild).GetAwaiter().GetResult();
                    updateController.Set();
                });
                return Task.CompletedTask;
            };

            System.Timers.Timer stopTimer = new System.Timers.Timer(new TimeSpan(0, 10, 0).TotalMilliseconds);
            System.Timers.Timer updateTimer = new System.Timers.Timer(new TimeSpan(0, 0, 10).TotalMilliseconds); //MessageFormat.UpdateList раз в 15 минут

            stopTimer.Elapsed += async (s, e) =>
            {
                try
                {
                    var connections = lavalink.ConnectedNodes.First().Value.ConnectedGuilds.Values.ToArray();
                    connController.WaitOne();
                    Parallel.ForEach(connections, async e => 
                    {
                        LavalinkGuildConnection connection = e;
                        byte counter;
                        string number = guildConnections[connection].Value;
                        if (connection.IsConnected && guildConnections.ContainsKey(connection))
                        {
                            counter = guildConnections[connection].Key;
                            DiscordChannel channel = connection.Channel;
                            if (channel.Users.Count == 1 && counter == 1)
                            {
                                if (connection.IsConnected)
                                    await connection.DisconnectAsync();
                                guildConnections.Remove(connection);
                            }
                            else if (channel.Users.Count == 1 && counter == 0)
                            {
                                guildConnections.Remove(connection);
                                guildConnections.Add(connection, new KeyValuePair<byte, string>(++counter, number));
                            }
                            else if (channel.Users.Count > 1 && counter != 0)
                            {
                                guildConnections.Remove(connection);
                                guildConnections.Add(connection, new KeyValuePair<byte, string>(--counter, number));
                            }
                        }
                        else if (!connection.IsConnected && guildConnections.ContainsKey(connection))
                        {
                            guildConnections.Remove(connection);
                        }
                        else if (connection.IsConnected && !guildConnections.ContainsKey(connection))
                        {
                            guildConnections.Add(connection, new KeyValuePair<byte, string>(0, number));
                        }
                    });
                    connController.Set();
                    //for (int i = 0; i < connections.Count(); i++)
                    //{
                    //    connController.WaitOne();
                    //    LavalinkGuildConnection connection = connections[i];
                    //    byte counter;
                    //    string number = guildConnections[connection].Value;
                    //    if (connection.IsConnected && guildConnections.ContainsKey(connection))
                    //    {
                    //        counter = guildConnections[connection].Key;
                    //        DiscordChannel channel = connection.Channel;
                    //        if (channel.Users.Count == 1 && counter == 1)
                    //        {
                    //            if (connection.IsConnected)
                    //                await connection.DisconnectAsync();
                    //            guildConnections.Remove(connection);
                    //        }
                    //        else if (channel.Users.Count == 1 && counter == 0)
                    //        {
                    //            guildConnections.Remove(connection);
                    //            guildConnections.Add(connection, new KeyValuePair<byte, string>(++counter, number));
                    //        }
                    //        else if (channel.Users.Count > 1 && counter != 0)
                    //        {
                    //            guildConnections.Remove(connection);
                    //            guildConnections.Add(connection, new KeyValuePair<byte, string>(--counter, number));
                    //        }
                    //    }
                    //    else if (!connection.IsConnected && guildConnections.ContainsKey(connection))
                    //    {
                    //        guildConnections.Remove(connection);
                    //    }
                    //    else if (connection.IsConnected && !guildConnections.ContainsKey(connection))
                    //    {
                    //        guildConnections.Add(connection, new KeyValuePair<byte, string>(0, number));
                    //    }
                    //    connController.Set();
                    //}
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(ex.Message);
                    Console.ForegroundColor = ConsoleColor.White;
                    connController.Set();
                }
            };
            updateTimer.Elapsed += (s, e) =>
            {
                updateController.WaitOne();
                Parallel.ForEach(client.Guilds.Values, async (guild) => await MessageFormat.UpdateList(guild));
                updateController.Set();
            };
            stopTimer.Start();
            updateTimer.Start();

            updateController.WaitOne();
            Parallel.ForEach(client.Guilds.Values, async (guild) => await MessageFormat.UpdateList(guild));
            updateController.Set();
            await Task.Delay(-1);
        }
        static void MainTg()
        {
            string tgToken = "";
            using (StreamReader reader = new StreamReader("tgToken.txt"))
            {
                tgToken = reader.ReadToEnd();
            }
            TelegramBotClientOptions opt = new TelegramBotClientOptions(tgToken);
            TelegramBotClient tgClient = new TelegramBotClient(opt);
            CancellationTokenSource cts = new CancellationTokenSource();
            ReceiverOptions receiverOptions = new()
            {
                AllowedUpdates = Array.Empty<UpdateType>() // receive all update types except ChatMember related updates
            };
            tgClient.StartReceiving(
                updateHandler: TgUpdateHandler,
                pollingErrorHandler: HandlePollingErrorAsync,
                receiverOptions: receiverOptions,
                cancellationToken: cts.Token
                );
        }
        static async Task TgUpdateHandler(ITelegramBotClient client, Update update, CancellationToken token)
        {
            string serverBtn = "\u2139";
            string usersBtn = "👤";
            string password = "29032005";
            ReplyKeyboardMarkup keyboard = new(new[]
                {
                    new KeyboardButton[]{$"Users online {usersBtn}", $"Servers info {serverBtn}"},
                })
            {
                ResizeKeyboard = true
            };
            if (update.Message != null)
            {
                if (update.Message.Type == TgMessageType.Text)
                {
                    if (update.Message.Text == "/start")
                    {
                        await client.SendTextMessageAsync(
                            chatId: update.Message.Chat.Id,
                            text: "Send password",
                            cancellationToken: token);
                    }
                    else if (update.Message.Text == password)
                    {
                        await client.SendTextMessageAsync(
                            chatId: update.Message.Chat.Id,
                            replyMarkup: keyboard,
                            text: "Press button",
                            cancellationToken: token);
                    }
                    else if (update.Message.Text == $"Users online {usersBtn}")
                    {
                        await client.SendTextMessageAsync(
                            chatId: update.Message.Chat.Id,
                            replyMarkup: keyboard,
                            text: $"Users online count equals {Program.client.GetLavalink().ConnectedNodes.First().Value.ConnectedGuilds.Count}",
                            cancellationToken: token);
                    }
                    else if (update.Message.Text == $"Servers info {serverBtn}")
                    {
                        await client.SendTextMessageAsync(
                            chatId: update.Message.Chat.Id,
                            replyMarkup: keyboard,
                            text: $"Servers count equals {Program.client.Guilds.Count.ToString()}",
                            cancellationToken: token);
                    }
                }
            }
            return;
        }
        static Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };


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
                MinimumLogLevel = LogLevel.Debug,
                Intents = DiscordIntents.AllUnprivileged, //DiscordIntents.AllUnprivileged
                AutoReconnect = true
            };

            using (StreamReader reader = new StreamReader("Token.txt"))
            {
                config.Token = reader.ReadLine();
            }

            client = new DiscordClient(config);
            number = Int32.Parse(MessageFormat.createList(MessageFormat.TakeRadioStreams()).Substring(0, 1));

            guildConnections = new Dictionary<LavalinkGuildConnection, KeyValuePair<byte, string>>();
        }
        public static DiscordClient client;
        static DiscordConfiguration config;
        static LavalinkConfiguration lavalinkConfig;
        static ConnectionEndpoint endpoint;
        public static int number;//номер первой станции
        public static AutoResetEvent connController = new AutoResetEvent(true);
        internal static Dictionary<LavalinkGuildConnection, KeyValuePair<byte, string>> guildConnections;
    }
}
