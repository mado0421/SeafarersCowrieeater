namespace SeafarersCowrieeater.Tests;

[TestFixture]
public class DiscordMessageSenderTests
{
    [Test]
    public void SendTweetAsync_WhenNotConnected_ThrowsInvalidOperationException()
    {
        // Arrange
        // ConnectAsync 없이 DiscordMessageSender를 생성하기만 함
        var messageSender = new DiscordMessageSender("fake-token");
        var emptyTweet = TweetData.Empty;

        // Act & Assert
        // 연결되지 않은 상태에서 SendTweetAsync 호출 시 InvalidOperationException이 발생하는지 확인
        Assert.ThrowsAsync<InvalidOperationException>(async () => await messageSender.SendTweetAsync(emptyTweet));
    }

    [Test]
    public void BuildEmbedsForTweet_WithMultipleImages_CreatesCorrectEmbeds()
    {
        // Arrange
        var tweet = new TweetData(
            "12345", 
            "테스트 트윗입니다.", 
            "TestUser",
            ["http://image.com/1.jpg", "http://image.com/2.jpg"]);
        
        var expectedTweetLink = "https://twitter.com/TestUser/status/12345";

        // Act
        var embeds = DiscordMessageSender.BuildEmbedsForTweet(tweet);

        // Assert
        Assert.That(embeds, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            // 첫 번째 임베드 검증 (제목, 본문, 이미지 포함)
            var firstEmbed = embeds[0];
            Assert.That(firstEmbed.Title, Is.EqualTo("R'ubia Neko님의 이번주 개척 공방 무인도 산품 무역 – 주간 스케줄"));
            Assert.That(firstEmbed.Description, Is.EqualTo(tweet.Text));
            Assert.That(firstEmbed.Url, Is.EqualTo(expectedTweetLink));
            Assert.That(firstEmbed.Image.HasValue, Is.True);
            Assert.That(firstEmbed.Image!.Value.Url, Is.EqualTo("http://image.com/1.jpg"));
            Assert.That(firstEmbed.Color.HasValue, Is.True);
            Assert.That(firstEmbed.Color!.Value.RawValue, Is.EqualTo(0x1DA1F2));

            // 두 번째 임베드 검증 (이미지만 포함)
            var secondEmbed = embeds[1];
            Assert.That(secondEmbed.Title, Is.Null.Or.Empty);
            Assert.That(secondEmbed.Description, Is.Null.Or.Empty);
            Assert.That(secondEmbed.Url, Is.EqualTo(expectedTweetLink)); // 링크는 동일하게 유지
            Assert.That(secondEmbed.Image.HasValue, Is.True);
            Assert.That(secondEmbed.Image!.Value.Url, Is.EqualTo("http://image.com/2.jpg"));
        });
    }

    [Test]
    public void BuildEmbedsForTweet_WithNoImages_CreatesSingleEmbed()
    {
        // Arrange
        var tweet = new TweetData("54321", "이미지 없는 트윗", "TestUser2", []);

        // Act
        var embeds = DiscordMessageSender.BuildEmbedsForTweet(tweet);

        // Assert
        Assert.That(embeds, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            var embed = embeds[0];
            Assert.That(embed.Title, Is.EqualTo("R'ubia Neko님의 이번주 개척 공방 무인도 산품 무역 – 주간 스케줄"));
            Assert.That(embed.Description, Is.EqualTo(tweet.Text));
            Assert.That(embed.Url, Is.EqualTo("https://twitter.com/TestUser2/status/54321"));
            Assert.That(embed.Image, Is.Null, "이미지가 없어야 합니다.");
            Assert.That(embed.Color.HasValue, Is.True);
        });
    }

    [Test]
    public void BuildEmbedsForTweet_WithEmptyTweetId_UsesFallbackUrl()
    {
        // Arrange
        var tweet = new TweetData("", "ID가 없는 트윗", "TestUser3", []);

        // Act
        var embeds = DiscordMessageSender.BuildEmbedsForTweet(tweet);
        
        // Assert
        Assert.That(embeds, Has.Count.EqualTo(1));
        Assert.That(embeds[0].Url, Is.EqualTo("https://x.com/nessie7737/status/1883724107482878453"));
    }
}
