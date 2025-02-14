using Newtonsoft.Json.Linq;

namespace SeafarersCowrieeater;

public class TweetGetter
{
    public async Task<TweetData> GetTweet(string token, string twitterId, List<string> keywords)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

        var endpoint =
                $"https://api.twitter.com/2/tweets/search/recent?query=from:{twitterId}%20%23무인도의고양이%20Week%20has:images&max_results=100&expansions=attachments.media_keys&media.fields=url";
        
        var response = await client.GetAsync(endpoint);

        if (response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            var jObject = JObject.Parse(responseBody);

            var tweets = jObject["data"];
            var mediaIncludes = jObject["includes"]?["media"];

            if (tweets is null)
                return TweetData.Empty;

            foreach (var tweet in tweets)
            {
                var text = tweet["text"]?.ToString() ?? string.Empty;
                if (keywords.Any(keyword => !text.Contains(keyword))) continue;

                var tweetId = tweet["id"]?.ToString() ?? string.Empty;

                List<string> mediaKeys = tweet["attachments"]?["media_keys"]?
                    .Select(jToken => jToken.ToString())
                    .ToList() ?? new List<string>();
                var mediaUrls = mediaKeys.Any() && mediaIncludes is not null
                    ? mediaIncludes
                        .Where(jToken => mediaKeys.Contains(jToken["media_key"]?.ToString() ?? string.Empty))
                        .Select(jToken => jToken["url"]?.ToString() ?? string.Empty).ToList()
                    : new List<string>();

                return new TweetData(tweetId, mediaUrls);
            }

            return TweetData.Empty;
        }

        // 응답 헤더에서 필요한 값 추출
        var limit = response.Headers.Contains("x-rate-limit-limit")
            ? response.Headers.GetValues("x-rate-limit-limit").FirstOrDefault()
            : null;

        var remaining = response.Headers.Contains("x-rate-limit-remaining")
            ? response.Headers.GetValues("x-rate-limit-remaining").FirstOrDefault()
            : null;

        var resetTimestamp = response.Headers.Contains("x-rate-limit-reset")
            ? response.Headers.GetValues("x-rate-limit-reset").FirstOrDefault()
            : null;

        // 값 출력
        Console.WriteLine($"Rate Limit: {limit}");
        Console.WriteLine($"Remaining Requests: {remaining}");

        // resetTimestamp가 null일 경우 기본값 처리
        if (string.IsNullOrEmpty(resetTimestamp) || !long.TryParse(resetTimestamp, out var resetTimeUnix))
        {
            Console.WriteLine("Rate Limit Reset Time: Unknown");
        }
        else
        {
            var resetTime = DateTimeOffset.FromUnixTimeSeconds(resetTimeUnix).UtcDateTime;
            Console.WriteLine($"Rate Limit Reset at: {resetTime.ToLocalTime()}");
        }

        var errorDetails = await response.Content.ReadAsStringAsync();
        throw new HttpRequestException($"Error {response.StatusCode}: {errorDetails}");
    }

    public class TweetData
    {
        private TweetData()
        {
            Id = string.Empty;
            MediaUrls = new List<string>();
        }

        public TweetData(string id, List<string> mediaUrls)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            Id = id;
            MediaUrls = mediaUrls;
        }

        public static TweetData Empty { get; } = new();
        public bool IsEmpty => string.IsNullOrEmpty(Id);

        public string Id { get; }
        public List<string> MediaUrls { get; }
    }
}
