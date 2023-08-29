using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using Newtonsoft.Json;
using System.Text;
using System.Threading.Channels;

namespace Bot
{
    static class MessageFormat
    {
        static MessageFormat()
        {
            using (StreamReader reader = new StreamReader("message.txt"))
            {
                adminMsg = reader.ReadToEnd();
            }
            file = new FileInfo("message.txt");
        }
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
                string name;
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
        public async static Task UpdateList(object gl)
        {
            try
            {
                DiscordGuild guild = (DiscordGuild)gl;
                DiscordChannel channel = await FindChannel(guild);
                IEnumerable<DiscordMessage> messages = await channel.GetMessagesAsync();


                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"update {channel.Guild.Name}");
                Console.ForegroundColor = ConsoleColor.White;


                Dictionary<string, Dictionary<string, string>> RadioStreams = null;
                while (RadioStreams == null)
                    RadioStreams = TakeRadioStreams();

                string message = "";

                if (messages.Any())
                {
                    messages = await Filter(messages);
                    message = GetMessageContentSum(messages);
                }
                if ((DateTime.Now - file.LastWriteTime).TotalHours > 1)
                {
                    using (StreamReader reader = new StreamReader("message.txt"))
                    {
                        adminMsg = await reader.ReadToEndAsync();
                    }
                }//проверка сообщения снизу раз в час

                string currentMessage = createList(RadioStreams) + '\r' + adminMsg;
                if (message != currentMessage)
                {
                    Stack<DiscordMessage> messageStack = new Stack<DiscordMessage>();
                    string list = createList(RadioStreams);

                    //номер первой станции
                    string strNumber = list.Substring(0, 1);
                    Program.number = Int32.Parse(strNumber);
                    if(messages.Any())
                        await DeleteMessages(messages);
                    await SendList(channel);
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.ForegroundColor = ConsoleColor.White;
            }
        }
        async static Task<DiscordChannel> FindChannel(DiscordGuild guild)
        {
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

            return channel;
        }
        async static Task<IEnumerable<DiscordMessage>> Filter(IEnumerable<DiscordMessage> messages)
        {
            Stack<DiscordMessage> messageStack = new Stack<DiscordMessage>();
            DiscordChannel channel;

            channel = messages.First().Channel;
            var delMessages = messages.Where(m =>
            {
                if ((m.Author == Program.client.CurrentUser && m.Content.Length < 100 && m.Content != adminMsg) || m.Author != Program.client.CurrentUser)
                    return true;
                return false;
            });

            messages = messages.Where(m =>
            {
                if (m.Author == Program.client.CurrentUser && m.Content.Length > 100 || m.Content == adminMsg)
                    return true;
                return false;
            });
            if(delMessages.Any())
                await DeleteMessages(delMessages);
            return messages;
        }
        async static Task DeleteMessages(IEnumerable<DiscordMessage> messages)
        {
            Stack<DiscordMessage> messageStack = new Stack<DiscordMessage>();
            DiscordChannel channel = messages.First().Channel;

            foreach (DiscordMessage msg in messages)
                if ((DateTime.Now - msg.Timestamp.DateTime).TotalDays > 13)
                    await msg.DeleteAsync();
                else
                    messageStack.Push(msg);

            if (messageStack.Any())
                await channel.DeleteMessagesAsync(messageStack);
        }
        static string GetMessageContentSum(IEnumerable<DiscordMessage> messages)
        {
            Stack<DiscordMessage> messageStack = new Stack<DiscordMessage>();
            string mess = "";
            foreach (var a in messages)
                messageStack.Push(a);
            foreach (var a in messageStack)
            {
                mess += a.Content;
                mess += "\r";
            }
            return mess.Trim();

        }
        async static Task SendList(DiscordChannel channel)
        {
            DiscordSelectComponent selectMenu;
            DiscordButtonComponent button;
            string list = createList(TakeRadioStreams());

            //номер первой станции
            string strNumber = list.Substring(0, 1);
            Program.number = Int32.Parse(strNumber);

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
            await channel.SendMessageAsync(adminMsg);
        }
        static string adminMsg;
        static FileInfo file;
    }
}
