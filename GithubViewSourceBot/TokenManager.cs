namespace GithubViewSourceBot {
    public class TokenManager {
        public TokenManager() {
            DiscordToken = "";
        }

        public string DiscordToken { get; private set; }
    }
}