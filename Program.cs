using SeafarersCowrieeater;

var tweetGetter = new TweetGetter();
var discordMessageSender = new DiscordMessageSender();
List<string> keywords = new List<string> { "#무인도의고양이", "Week" };

// 환경 변수 값 읽기
string? twitterToken = Environment.GetEnvironmentVariable("TWITTER_TOKEN");
string? discordToken = Environment.GetEnvironmentVariable("DISCORD_TOKEN");
string? twitterId = Environment.GetEnvironmentVariable("TWITTER_ID");

// null 확인 및 기본값 처리
if (string.IsNullOrEmpty(twitterToken) || string.IsNullOrEmpty(discordToken) || string.IsNullOrEmpty(twitterId))
{
    Console.WriteLine("필수 환경 변수가 설정되지 않았습니다.");
    return;
}
            
var tweet = await tweetGetter.GetTweet(twitterToken, twitterId, keywords);
await discordMessageSender.SendAsync(discordToken, twitterId, tweet.Id, tweet.MediaUrls);