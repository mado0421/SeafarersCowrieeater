using Newtonsoft.Json.Linq;

namespace SeafarersCowrieeater;

public class TweetGetter
{
    public class TweetData
    {
        public static TweetData Empty { get; } = new();
        public bool IsEmpty => string.IsNullOrEmpty(Text);

        private TweetData()
        {
            Text = string.Empty;
        }

        public TweetData(string text, Stream? image = null)
        {
            if (string.IsNullOrEmpty(text)) throw new ArgumentNullException(nameof(text));

            Text = text;
            Image = image;
        }

        public string Text { get; }
        public Stream? Image { get; }
    }

    public async Task<TweetData> GetTweet(string token, string id, List<string> keywords)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

        var endpoint = $"https://api.twitter.com/2/users/{id}/tweets";
        var response = await client.GetAsync(endpoint);

        if (response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            var tweets = JObject.Parse(responseBody)["data"];
            if (tweets is null) return TweetData.Empty;

            foreach (var tweet in tweets)
            {
                var text = tweet["text"]?.ToString() ?? string.Empty;
                var imageUrl = tweet["attachments"]?["media_keys"]?.First?.ToString(); // 이미지 URL 추출

                if (keywords.Any(keyword => !text.Contains(keyword))) continue;
                if (string.IsNullOrEmpty(imageUrl)) continue;

                using var httpClient = new HttpClient();
                return new TweetData(text, await httpClient.GetStreamAsync(imageUrl));
            }

            return TweetData.Empty;
        }
        else
        {
            var errorDetails = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Error {response.StatusCode}: {errorDetails}");
        }
    }
}