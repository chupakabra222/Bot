using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using DSharpPlus.Net;
using DSharpPlus.SlashCommands;

using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.IO.Pipes;

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
						userCount++;
						string number = e.Interaction.Data.Values.First();
						if (invoker.VoiceState == null)
						{
							await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Вы не подключены к какому-либо голосовому каналу"));
							return;
						}
						DiscordChannel channel = invoker.VoiceState.Channel;
						Player.StartPlay(client, invoker, number);
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


			System.Timers.Timer updateTimer = new System.Timers.Timer(900000); //MessageFormat.UpdateList раз в 15 минут
			System.Timers.Timer infoTimer = new System.Timers.Timer(900000);
			updateTimer.Elapsed += async (s, e) =>
			{
				controller.WaitOne();

				foreach (DiscordGuild guild in client.Guilds.Values)
					await MessageFormat.UpdateList(guild);

				controller.Set();
			};
			infoTimer.Elapsed += (s, e) =>
			{
				Console.ForegroundColor = ConsoleColor.Green;
				Console.WriteLine($"Users count equals {userCount}");
				Console.ForegroundColor = ConsoleColor.White;
			};

			updateTimer.Start();
			infoTimer.Start();
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
		}
		public static DiscordClient client;
		static DiscordConfiguration config;
		static LavalinkConfiguration lavalinkConfig;
		static ConnectionEndpoint endpoint;
		public static int number;//номер первой станции
		static int userCount;
	}
}