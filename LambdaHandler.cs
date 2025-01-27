using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace SeafarersCowrieeater;

public class LambdaHandler
{
    public async Task FunctionHandler(InitializeEvent initializeEvent, ILambdaContext context)
    {
        Console.WriteLine($"{initializeEvent}, {initializeEvent.IsDiscordTest}");
        
        
        var tweetGetter = new TweetGetter();
        var discordMessageSender = new DiscordMessageSender();
        List<string> keywords = new List<string> { "#무인도의고양이", "Week" };

        // 환경 변수 값 읽기
        var twitterToken = Environment.GetEnvironmentVariable("TWITTER_TOKEN");
        var discordToken = Environment.GetEnvironmentVariable("DISCORD_TOKEN");
        var twitterId = Environment.GetEnvironmentVariable("TWITTER_ID");

        // null 확인 및 기본값 처리
        if (string.IsNullOrEmpty(twitterToken) || string.IsNullOrEmpty(discordToken) ||
            string.IsNullOrEmpty(twitterId))
        {
            Console.WriteLine("필수 환경 변수가 설정되지 않았습니다.");
            return;
        }

        if (initializeEvent.IsDiscordTest)
        {
            // DiscordMessageSender 호출
            // await discordMessageSender.SendAsync(discordToken, twitterId, "1881707481107480957", [
            //     "https://pbs.twimg.com/media/Gh0q-pFa8AAqCdd?format=jpg&name=large",
            //     "https://pbs.twimg.com/media/Gh0q-pDa0AA-_uY?format=jpg&name=large",
            //     "https://pbs.twimg.com/media/Gh0q-pIbYAAP5qL?format=jpg&name=large"
            // ]);
            await discordMessageSender.SendAsync(discordToken, twitterId, "", [
                "https://pbs.twimg.com/media/GiRVF7ZbkAAZ8VY?format=jpg&name=small",
                "https://pbs.twimg.com/media/GiRVF9YaAAAzh9I?format=jpg&name=medium"
            ]);
        }
        else
        {
            var tweet = await tweetGetter.GetTweet(twitterToken, twitterId, keywords);
            await discordMessageSender.SendAsync(discordToken, twitterId, tweet.Id, tweet.MediaUrls);
        }
    }

    public class InitializeEvent
    {
        public bool IsDiscordTest { get; set; }
    }
}