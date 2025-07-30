using System.Net.Http.Headers;
using Newtonsoft.Json;

namespace SeafarersCowrieeater;

public class TweetGetter(HttpClient httpClient)
{
    private const string TwitterApiBaseUrl = "https://api.twitter.com/2/tweets/search/recent";
    private const string DefaultQueryParameters = "max_results=100&expansions=attachments.media_keys&media.fields=url";

    private string BuildTwitterEndpoint(string authorId, IEnumerable<string> keywords)
    {
        var keywordQuery = string.Join(" ", keywords);
        var rawQuery = $"from:{authorId} has:images {keywordQuery}".Trim();
        var encodedQuery = Uri.EscapeDataString(rawQuery);

        return $"{TwitterApiBaseUrl}?query={encodedQuery}&{DefaultQueryParameters}";
    }
    
    public async Task<TweetData> GetTweetAsync(string token, string authorId, List<string> keywords)
    {
        Console.WriteLine("[TweetGetter] Starting to fetch tweet...");
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        
        var endpoint = BuildTwitterEndpoint(authorId, keywords);
        Console.WriteLine($"[TweetGetter] Requesting Twitter API with endpoint: {endpoint}");
        
        var response = await httpClient.GetAsync(endpoint);

        var responseBody = await response.Content.ReadAsStringAsync();
        
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"[TweetGetter] ERROR: Twitter API request failed. Status: {response.StatusCode}. Body: {responseBody}");
            throw new HttpRequestException($"Twitter API request failed with status code {response.StatusCode}: {responseBody}");
        }

        Console.WriteLine("[TweetGetter] Successfully received response from Twitter API.");
        var twitterResponse = JsonConvert.DeserializeObject<TwitterApiResponse>(responseBody);

        return FindBestMatchingTweet(twitterResponse, authorId, keywords);
    }

    private TweetData FindBestMatchingTweet(TwitterApiResponse? response, string authorId, List<string> keywords)
    {
        Console.WriteLine("[TweetGetter] Finding best matching tweet from API response...");
        
        if (keywords.Count == 0)
        {
            Console.WriteLine("[TweetGetter] WARN: No keywords were provided to filter tweets. Returning no match as per policy.");
            return TweetData.Empty;
        }

        if (response?.Data == null || !response.Data.Any())
        {
            Console.WriteLine("[TweetGetter] WARN: Twitter API response contained no tweets (response.Data is null or empty).");
            return TweetData.Empty;
        }

        Console.WriteLine($"[TweetGetter] Received {response.Data.Count} tweets from API. Filtering now...");

        var mediaDictionary = response.Includes?.Media?.ToDictionary(m => m.MediaKey) 
                              ?? new Dictionary<string, TwitterMedia>();

        foreach (var tweet in response.Data)
        {
            var formattedText = tweet.Text.Replace("\n", " ").Trim();
            Console.WriteLine($"[TweetGetter] -> Checking tweet ID: {tweet.Id}, Text: \"{formattedText}\"");
            
            // 키워드 필터링: 모든 키워드를 포함해야 함
            if (keywords.Any(keyword => !tweet.Text.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
            {
                Console.WriteLine($"[TweetGetter]    - Tweet {tweet.Id} did not match all keywords. Skipping.");
                continue;
            }
            
            Console.WriteLine($"[TweetGetter]    + SUCCESS: Tweet {tweet.Id} matches all keywords.");
            
            var mediaUrls = tweet.Attachments?.MediaKeys?
                .Select(key => mediaDictionary.TryGetValue(key, out var media) ? media.Url : null)
                .Where(url => url != null)
                .ToList();

            return new TweetData(tweet.Id, tweet.Text, authorId, mediaUrls!);
        }

        Console.WriteLine("[TweetGetter] WARN: Finished checking all received tweets, but none matched the criteria.");
        return TweetData.Empty;
    }
}


// --- API 응답을 위한 강타입 모델들 ---

public class TwitterApiResponse
{
    [JsonProperty("data")]
    public List<TwitterTweet>? Data { get; set; }

    [JsonProperty("includes")]
    public TwitterIncludes? Includes { get; set; }
}

public class TwitterTweet
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("text")]
    public string Text { get; set; } = string.Empty;

    [JsonProperty("attachments")]
    public TwitterAttachments? Attachments { get; set; }
}

public class TwitterAttachments
{
    [JsonProperty("media_keys")]
    public List<string>? MediaKeys { get; set; }
}

public class TwitterIncludes
{
    [JsonProperty("media")]
    public List<TwitterMedia>? Media { get; set; }
}

public class TwitterMedia
{
    [JsonProperty("media_key")]
    public string MediaKey { get; set; } = string.Empty;

    [JsonProperty("url")]
    public string? Url { get; set; }
}

public class TweetData(string id, string text, string authorId, List<string> mediaUrls)
{
    private TweetData() : this(string.Empty, string.Empty, string.Empty, []) { }

    public static TweetData Empty { get; } = new();
    public bool IsEmpty => string.IsNullOrEmpty(Id);

    public string Id { get; } = id;
    public string Text { get; } = text;
    public string AuthorId { get; } = authorId;
    public List<string> MediaUrls { get; } = mediaUrls;
}