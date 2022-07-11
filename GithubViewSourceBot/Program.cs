using System.Reflection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace GithubViewSourceBot {
    class Program {

        static void Main() {
            new Program().MainAsync().GetAwaiter().GetResult();
        }

        private DiscordSocketClient _client;
        private CommandService _commands;
        private IServiceProvider _services;
        private TokenManager _token;

        public async Task MainAsync() {
            _services = ConfigureServices();
            _client = _services.GetRequiredService<DiscordSocketClient>();
            _client.Log += Log;
            _services.GetRequiredService<CommandService>().Log += Log;
            _commands = new CommandService();
            _client.MessageReceived += CommandReceived;
            _token = new TokenManager();
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
            await _client.LoginAsync(TokenType.Bot, _token.DiscordToken);
            await _client.StartAsync();
            await _client.SetGameAsync(null);
            await Task.Delay(-1);
        }

        private async Task CommandReceived(SocketMessage messageParam) {
            var message = messageParam as SocketUserMessage;
            if (message == null) return;
            if (message.Author.IsBot) return;

            try {
                var uri = new Uri(message.Content);
                if (uri.Host != "github.com") return;
                var uri_info = uri.AbsolutePath.Split('/');
                if (uri_info.Length <= 3) return;

                var user = uri_info[1];
                var repo = uri_info[2];
                var blob = uri_info[3];
                var branch = uri_info[4];
                var array_last_key = uri_info.Length-1;
                var split_file_name = uri_info[array_last_key].Split('.');
                var ext = split_file_name.Length == 0 ? "" : split_file_name[split_file_name.Length - 1];

                var raw_url = "https://raw.githubusercontent.com/" + user + '/'+ repo+ "/" + branch + "/";
                var pathes = new List<string>();
                for (var i = 5; i < uri_info.Length; i++) {
                    pathes.Add(uri_info[i]);
                }
                var path = string.Join("/", pathes);
                raw_url += path;

                if (uri.Fragment == string.Empty || uri.Fragment == "#") return;

                var fragment = uri.Fragment.Replace("#", "").Replace("L", "");
                var fragment_split = fragment.Split('-');

                var start = uri.Fragment.Contains("-") ? fragment_split[0] : fragment;
                var end = uri.Fragment.Contains("-") ? fragment_split[1] : start;
                var send_message = "```" + ext + "\n";


                using (var webClient = new System.Net.WebClient()) {
                    var content = webClient.DownloadString(raw_url);
                    var contentList = content.Replace("\r\n", "\n").Split(new[] { '\n', '\r' });
                    if (contentList[0] == "404: Not Found") return;
                    if (Int32.Parse(start) > Int32.Parse(end)) return;
                    var loop_counter = 1;

                    for (int i = Int32.Parse(start) - 1; i  <= Int32.Parse(end) - 1; i++) {
                        if (contentList.Length - 1 <= i) break;
                        if (loop_counter >= 30) break;
                        send_message += (i + 1) + " " + contentList[i] + "\n";
                        loop_counter++;
                    }
                    send_message += "```";
                    if (contentList.Length != 0) {
                        message.Channel.SendMessageAsync(send_message);
                    }
                }
            } catch(Exception e) {
            }
        }

        private ServiceProvider ConfigureServices() {
            return new ServiceCollection()
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton<CommandService>()
                .BuildServiceProvider();
        }

        private Task Log(LogMessage message) {
            Console.WriteLine(message.ToString());
            return Task.CompletedTask;
        }
    }
}