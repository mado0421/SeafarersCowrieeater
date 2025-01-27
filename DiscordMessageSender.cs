using Discord;
using Discord.WebSocket;

namespace SeafarersCowrieeater;

public class DiscordMessageSender
{
    public async Task SendAsync(string token, string tweetAccountId, string tweetId, List<string> imageUrls)
    {
        var taskCompletionSource = new TaskCompletionSource();
        var client = new DiscordSocketClient(
            new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.Guilds |
                                 GatewayIntents.DirectMessages |
                                 GatewayIntents.GuildMessages
            }
        );
        
        client.Log += OnLog;
        client.Ready += async () =>
        {
            IReadOnlyCollection<SocketTextChannel> textChannels = GetTextChannels(client);

            if (!string.IsNullOrEmpty(tweetId))
            {
                var tweetLink = $"https://twitter.com/{tweetAccountId}/status/{tweetId}";
                foreach (var textChannel in textChannels) await textChannel.SendMessageAsync(tweetLink);
            }

            foreach (var textChannel in textChannels)
            foreach (var imageUrl in imageUrls)
                await textChannel.SendMessageAsync(imageUrl);

            await client.LogoutAsync();
            await client.StopAsync();

            // Ready 핸들러가 끝났음을 알림
            taskCompletionSource.SetResult();
        };

        await client.LoginAsync(TokenType.Bot, token);
        await client.StartAsync();

        // Ready 핸들러가 끝날 때까지 대기
        await taskCompletionSource.Task;
    }

    private Task OnLog(LogMessage arg)
    {
        Console.WriteLine(arg.ToString());
        return Task.CompletedTask;
    }

    /// <summary>
    ///     SendMessages, AttachFiles, EmbedLinks must be Allow.
    /// </summary>
    private IReadOnlyCollection<SocketTextChannel> GetTextChannels(DiscordSocketClient client)
    {
        List<SocketTextChannel> textChannels = [];

        foreach (var server in client.Guilds)
        foreach (var textChannel in server.TextChannels)
        {
            var permissions = textChannel.GetPermissionOverwrite(client.CurrentUser);

            if (permissions is null) continue;
            if (permissions.Value.SendMessages is not PermValue.Allow) continue;
            if (permissions.Value.AttachFiles is not PermValue.Allow) continue;
            if (permissions.Value.EmbedLinks is not PermValue.Allow) continue;

            textChannels.Add(textChannel);
        }

        return textChannels;
    }
}