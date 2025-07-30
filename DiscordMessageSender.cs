using Discord;
using Discord.WebSocket;

namespace SeafarersCowrieeater;

public class DiscordMessageSender : IAsyncDisposable
{
    private readonly string _token; 
    private readonly DiscordSocketClient _client;
    private readonly TaskCompletionSource _readyTcs = new();


    // 생성자에서 클라이언트와 토큰을 설정합니다.
    public DiscordMessageSender(string token)
    {
        _token = token;
        _client = new DiscordSocketClient(new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages
        });
        _client.Log += OnLog;
        _client.Ready += OnClientReady;
    }

    // 1. 연결 책임만 담당하는 메서드
    public async Task ConnectAsync()
    {
        Console.WriteLine("[Discord] Logging in...");
        await _client.LoginAsync(TokenType.Bot, _token);
        Console.WriteLine("[Discord] Starting client...");
        await _client.StartAsync();

        Console.WriteLine("[Discord] Waiting for client to be ready...");
        // OnClientReady 핸들러에서 _readyTcs.TrySetResult()가 호출될 때까지 여기서 대기합니다.
        await _readyTcs.Task;
    }

    // 2. 메시지 전송 책임만 담당하는 메서드
    public async Task SendTweetAsync(TweetData tweet)
    {
        Console.WriteLine("[Discord] Preparing to send tweet message...");
        if (_client.LoginState != LoginState.LoggedIn)
        {
            Console.WriteLine("[Discord] ERROR: Client is not connected. Throwing exception.");
            throw new InvalidOperationException("클라이언트가 디스코드에 연결되지 않았습니다. ConnectAsync를 먼저 호출해주세요.");
        }
    
        var textChannels = GetWritableTextChannels();
        if (!textChannels.Any())
        {
            Console.WriteLine("[Discord] WARN: Could not find any channels where I can send messages.");
            return;
        }
        Console.WriteLine($"[Discord] Found {textChannels.Count} writable text channel(s).");
    
        var embeds = BuildEmbedsForTweet(tweet);
        if (!embeds.Any())
        {
            Console.WriteLine("[Discord] WARN: No embeds were built for the tweet. Nothing to send.");
            return;
        }

        foreach (var channel in textChannels)
        {
            Console.WriteLine($"[Discord] Sending message to channel: #{channel.Name} in guild: {channel.Guild.Name}");
            await channel.SendMessageAsync(embeds: embeds.Take(10).ToArray());
        }
        Console.WriteLine("[Discord] Successfully sent messages to all writable channels.");
    }

    public static List<Embed> BuildEmbedsForTweet(TweetData tweet)
    {
        var embeds = new List<Embed>();
        var tweetLink = !string.IsNullOrEmpty(tweet.Id)
            ? $"https://twitter.com/{tweet.AuthorId}/status/{tweet.Id}"
            : "https://x.com/nessie7737/status/1883724107482878453";

        // 이미지가 있는 경우
        if (tweet.MediaUrls.Any())
        {
            embeds.Add(new EmbedBuilder()
                .WithTitle("R'ubia Neko님의 이번주 개척 공방 무인도 산품 무역 – 주간 스케줄")
                .WithDescription(tweet.Text)
                .WithUrl(tweetLink)
                .WithImageUrl(tweet.MediaUrls.First())
                .WithColor(new Color(0x1DA1F2))
                .Build());
        
            embeds.AddRange(tweet.MediaUrls.Skip(1).Select(url => new EmbedBuilder().WithImageUrl(url).WithUrl(tweetLink).Build()));
        }
        else // 이미지가 없는 경우 (텍스트만)
        {
            embeds.Add(new EmbedBuilder()
                .WithTitle("R'ubia Neko님의 이번주 개척 공방 무인도 산품 무역 – 주간 스케줄")
                .WithDescription(tweet.Text)
                .WithUrl(tweetLink)
                .WithColor(new Color(0x1DA1F2))
                .Build());
        }
        return embeds;
    }


    // 3. 연결 해제 책임만 담당하는 메서드 (IDisposable 패턴)
    public async ValueTask DisposeAsync()
    {
        Console.WriteLine("[Discord] Disconnecting client...");
        await _client.LogoutAsync();
        await _client.StopAsync();
        await _client.DisposeAsync();
        Console.WriteLine("[Discord] Client disconnected.");
    }
    

    private Task OnLog(LogMessage arg)
    {
        // Discord.Net 라이브러리 자체에서 발생하는 로그
        Console.WriteLine($"[Discord.Net] {arg.ToString()}");
        return Task.CompletedTask;
    }
    
    // Ready 이벤트가 발생하면 이 메서드가 '단 한 번' 호출됩니다.
    private Task OnClientReady()
    {
        Console.WriteLine("[Discord] Client is ready!");
        _readyTcs.TrySetResult();
        // 이벤트가 다시 호출되지 않도록 핸들러를 제거합니다.
        _client.Ready -= OnClientReady;
        return Task.CompletedTask;
    }

    
    // 권한 확인 로직을 개선한 메서드
    private List<SocketTextChannel> GetWritableTextChannels()
    {
        var textChannels = new List<SocketTextChannel>();
        foreach (var guild in _client.Guilds)
        {
            var botUser = guild.GetUser(_client.CurrentUser.Id);
            if (botUser == null) continue;

            foreach (var channel in guild.TextChannels)
            {
                var permissions = botUser.GetPermissions(channel);
                if (permissions.Has(ChannelPermission.SendMessages) && 
                    permissions.Has(ChannelPermission.EmbedLinks))
                {
                    textChannels.Add(channel);
                }
            }
        }
        return textChannels;
    }
}