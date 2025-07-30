using SeafarersCowrieeater;



// 환경 변수 값 읽기
string? twitterToken = Environment.GetEnvironmentVariable("TWITTER_TOKEN");
string? discordToken = Environment.GetEnvironmentVariable("DISCORD_TOKEN");
string? twitterId = Environment.GetEnvironmentVariable("TWITTER_ID");

// null 확인 및 기본값 처리
if (string.IsNullOrEmpty(twitterToken)) Console.WriteLine("twitterToken 필수 환경 변수가 설정되지 않았습니다.");
if (string.IsNullOrEmpty(discordToken)) Console.WriteLine("discordToken 필수 환경 변수가 설정되지 않았습니다.");
if (string.IsNullOrEmpty(twitterId)) Console.WriteLine("twitterId 필수 환경 변수가 설정되지 않았습니다.");
if (string.IsNullOrEmpty(twitterToken) || string.IsNullOrEmpty(discordToken) || string.IsNullOrEmpty(twitterId))
    return;

List<string> keywords = ["#무인도의고양이", "주간"];

var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {twitterToken}");
var tweetGetter = new TweetGetter(httpClient);

await using var discordMessageSender = new DiscordMessageSender(discordToken);
await discordMessageSender.ConnectAsync();

var tweet = await tweetGetter.GetTweetAsync(twitterToken, twitterId, keywords);
await discordMessageSender.SendTweetAsync(tweet);
