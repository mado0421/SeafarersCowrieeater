using Discord;
using Discord.WebSocket;

namespace SeafarersCowrieeater;

public class DiscordMessageSender
{
    public async Task SendAsync(string token, string text, Stream? imagesStream)
    {
        DiscordSocketClient client = new DiscordSocketClient();
        await client.LoginAsync(TokenType.Bot, token);
        await client.StartAsync();

        var textChannels = GetTextChannels(client);

        if (imagesStream is null)
        {
            foreach (var textChannel in textChannels) 
                await textChannel.SendMessageAsync(text);
        }
        else
        {
            foreach (var textChannel in textChannels) 
                await textChannel.SendFileAsync(imagesStream, text);
        }

    }

    /// <summary>
    ///     SendMessages, AttachFiles, EmbedLinks must be Allow.
    /// </summary>
    private IReadOnlyCollection<SocketTextChannel> GetTextChannels(DiscordSocketClient client)
    {
        List<SocketTextChannel> textChannels = [];
        
        foreach (var server in client.Guilds)
        {
            foreach (var textChannel in server.TextChannels)
            {
                var permissions = textChannel.GetPermissionOverwrite(client.CurrentUser);
                
                if (permissions is null) continue;
                if (permissions.Value.SendMessages is not PermValue.Allow) continue;
                if (permissions.Value.AttachFiles is not PermValue.Allow) continue;
                if (permissions.Value.EmbedLinks is not PermValue.Allow) continue;
                
                textChannels.Add(textChannel);
            }
        }

        return textChannels;
    }
}