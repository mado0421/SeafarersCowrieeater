using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using System.Text.Json;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace SeafarersCowrieeater;

public class LambdaHandler
{
    public async Task FunctionHandler(InitializeEvent initializeEvent, ILambdaContext context)
    {
        Console.WriteLine("==================================================");
        Console.WriteLine("Lambda function execution started.");
        Console.WriteLine($"InitializeEvent received: {JsonSerializer.Serialize(initializeEvent)}");
        Console.WriteLine("==================================================");

        try
        {
            // 환경 변수 값 읽기
            Console.WriteLine("Step 1: Reading environment variables...");
            string? twitterToken = Environment.GetEnvironmentVariable("TWITTER_TOKEN");
            string? discordToken = Environment.GetEnvironmentVariable("DISCORD_TOKEN");
            string? authorId = Environment.GetEnvironmentVariable("TWITTER_ID");

            // null 확인 및 기본값 처리
            if (string.IsNullOrEmpty(twitterToken)) Console.WriteLine("[ERROR] twitterToken 필수 환경 변수가 설정되지 않았습니다.");
            if (string.IsNullOrEmpty(discordToken)) Console.WriteLine("[ERROR] discordToken 필수 환경 변수가 설정되지 않았습니다.");
            if (string.IsNullOrEmpty(authorId)) Console.WriteLine("[ERROR] twitterId 필수 환경 변수가 설정되지 않았습니다.");
            if (string.IsNullOrEmpty(twitterToken) || string.IsNullOrEmpty(discordToken) || string.IsNullOrEmpty(authorId))
            {
                Console.WriteLine("[FATAL] 필수 환경 변수 부족으로 함수를 종료합니다.");
                return;
            }
            Console.WriteLine("Step 1: Environment variables loaded successfully.");

            Console.WriteLine("Step 2: Initializing and connecting to Discord...");
            await using var discordMessageSender = new DiscordMessageSender(discordToken);
            await discordMessageSender.ConnectAsync();
            Console.WriteLine("Step 2: Discord client connected successfully.");

            Console.WriteLine("Step 3: Processing tweet logic...");
            if (initializeEvent.IsDiscordTest)
            {
                Console.WriteLine("[INFO] This is a Discord test run.");
                // 테스트 호출을 위해 임시 TweetData 객체를 생성합니다.
                var testTweet = new TweetData("", "이것은 디스코드 메시지 전송을 위한 테스트 트윗 본문입니다.", authorId, [
                        "https://pbs.twimg.com/media/GiRVF7ZbkAAZ8VY?format=jpg&name=small",
                        "https://pbs.twimg.com/media/GiRVF9YaAAAzh9I?format=jpg&name=medium"
                    ]);
                Console.WriteLine($"Sending test tweet (ID: {testTweet.Id}) to Discord.");
                await discordMessageSender.SendTweetAsync(testTweet);
                Console.WriteLine("Test tweet sent successfully.");
            }
            else
            {
                Console.WriteLine("[INFO] This is a standard tweet search run.");

                // --- 수정된 핵심 로직 ---
                // 이벤트에 Keywords가 포함되어 있는지 확인합니다.
                if (initializeEvent.Keywords == null || !initializeEvent.Keywords.Any())
                {
                    // Keywords가 없으면, 로그를 남기고 함수를 즉시 종료하여 API 호출을 방지합니다.
                    Console.WriteLine("[FATAL] No keywords provided in the Lambda event for a standard run. Aborting to prevent unnecessary Twitter API calls.");
                    return; 
                }
                
                Console.WriteLine($"[INFO] Using {initializeEvent.Keywords.Count} keywords from Lambda event: \"{string.Join("\", \"", initializeEvent.Keywords)}\"");

                var httpClient = new HttpClient();
                var tweetGetter = new TweetGetter(httpClient);
                var tweet = await tweetGetter.GetTweetAsync(twitterToken, authorId, initializeEvent.Keywords);
                
                if (tweet.IsEmpty)
                {
                    Console.WriteLine("[WARN] No matching tweet was found for the given criteria. No message will be sent to Discord this time.");
                }
                else
                {
                    Console.WriteLine($"Found matching tweet (ID: {tweet.Id}). Sending to Discord.");
                    await discordMessageSender.SendTweetAsync(tweet);
                }
            }
            Console.WriteLine("Step 3: Tweet logic processed successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FATAL] An unhandled exception occurred: {ex.Message}");
            Console.WriteLine($"[FATAL] Stack Trace: {ex.StackTrace}");
            // Lambda 실행이 실패했음을 CloudWatch에 알리기 위해 예외를 다시 던집니다.
            throw;
        }
        finally
        {
            Console.WriteLine("==================================================");
            Console.WriteLine("Lambda function execution finished.");
            Console.WriteLine("==================================================");
        }
    }

    public class InitializeEvent
    {
        public bool IsDiscordTest { get; set; }
        public List<string>? Keywords { get; set; }
    }
}