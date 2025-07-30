using System.Net;
using System.Text;
using Newtonsoft.Json;

namespace SeafarersCowrieeater.Tests;

[TestFixture]
public class TweetGetterTests
{
    // HttpClient의 응답을 흉내 내기 위한 가짜 HttpMessageHandler
    private class MockHttpMessageHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request; // 어떤 요청이 들어왔는지 저장
            return Task.FromResult(response);
        }
    }

    [Test]
    public async Task GetTweetAsync_WithValidResponse_ReturnsCorrectTweetData()
    {
        // Arrange (준비)
        var authorId = "TestUser123";
        var keywords = new List<string> { "#무인도의고양이" };
        var expectedTweetId = "12345";
        var expectedText = "이것은 #무인도의고양이 테스트 트윗입니다.";
        
        var apiResponse = new TwitterApiResponse
        {
            Data = [new TwitterTweet { Id = expectedTweetId, Text = expectedText }]
        };
        var jsonResponse = JsonConvert.SerializeObject(apiResponse);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
        };
        
        var mockHandler = new MockHttpMessageHandler(httpResponse);
        var httpClient = new HttpClient(mockHandler);
        var tweetGetter = new TweetGetter(httpClient);

        // Act (실행)
        var result = await tweetGetter.GetTweetAsync("fake-token", authorId, keywords);

        // Assert (검증)
        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.IsEmpty, Is.False);
            Assert.That(result.Id, Is.EqualTo(expectedTweetId));
            Assert.That(result.Text, Is.EqualTo(expectedText));
            Assert.That(result.AuthorId, Is.EqualTo(authorId));
        });

        var expectedRawQuery = "from:TestUser123 has:images #무인도의고양이";
        var expectedEncodedQuery = Uri.EscapeDataString(expectedRawQuery);
        Assert.That(mockHandler.LastRequest, Is.Not.Null);
        
        // StringAssert.Contains -> Assert.That(..., Does.Contain(...)) 으로 변경
        Assert.That(mockHandler.LastRequest!.RequestUri!.Query, Does.Contain(expectedEncodedQuery));
    }
    
    // --- 추가된 테스트 ---
    [Test]
    public async Task GetTweetAsync_WithEmptyKeywords_ReturnsEmptyTweetData()
    {
        // Arrange
        // API가 어떤 트윗을 반환하더라도, 키워드가 비어있으면 결과는 Empty여야 합니다.
        var apiResponse = new TwitterApiResponse { Data = [new TwitterTweet { Id = "123", Text = "Some tweet text" }] };
        var jsonResponse = JsonConvert.SerializeObject(apiResponse);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
        };

        var mockHandler = new MockHttpMessageHandler(httpResponse);
        var httpClient = new HttpClient(mockHandler);
        var tweetGetter = new TweetGetter(httpClient);

        // Act
        // 비어있는 키워드 리스트로 호출합니다.
        var result = await tweetGetter.GetTweetAsync("fake-token", "any-user", []);

        // Assert
        // 결과가 Empty인지 확인합니다.
        Assert.That(result.IsEmpty, Is.True);
    }
    // --- 여기까지 ---

    [Test]
    public async Task GetTweetAsync_WhenNoMatchingTweet_ReturnsEmptyTweetData()
    {
        // Arrange
        var apiResponse = new TwitterApiResponse { Data = [new TwitterTweet { Id = "123", Text = "다른 내용" }] };
        var jsonResponse = JsonConvert.SerializeObject(apiResponse);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
        };

        var mockHandler = new MockHttpMessageHandler(httpResponse);
        var httpClient = new HttpClient(mockHandler);
        var tweetGetter = new TweetGetter(httpClient);

        // Act
        var result = await tweetGetter.GetTweetAsync("fake-token", "any-user", ["non-matching-keyword"]);

        // Assert
        Assert.That(result.IsEmpty, Is.True);
    }

    [Test]
    public void GetTweetAsync_WhenApiReturnsError_ThrowsHttpRequestException()
    {
        // Arrange
        var httpResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("Invalid request")
        };

        var mockHandler = new MockHttpMessageHandler(httpResponse);
        var httpClient = new HttpClient(mockHandler);
        var tweetGetter = new TweetGetter(httpClient);

        // Act & Assert
        var ex = Assert.ThrowsAsync<HttpRequestException>(async () =>
            await tweetGetter.GetTweetAsync("fake-token", "any-user", []));
        
        // StringAssert.Contains -> Assert.That(..., Does.Contain(...)) 으로 변경
        Assert.That(ex!.Message, Does.Contain("failed with status code BadRequest"));
    }

    [Test]
    public async Task GetTweetAsync_WithSpecialCharacterKeywords_BuildsUrlCorrectly()
    {
        // Arrange
        var authorId = "AnotherUser";
        var keywords = new List<string> { "C#", "dev & test" };
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("""{ "data": [] }""") };
        
        var mockHandler = new MockHttpMessageHandler(httpResponse);
        var httpClient = new HttpClient(mockHandler);
        var tweetGetter = new TweetGetter(httpClient);
        
        // Act
        await tweetGetter.GetTweetAsync("fake-token", authorId, keywords);
        
        // Assert
        var expectedRawQuery = "from:AnotherUser has:images C# dev & test";
        var expectedEncodedQuery = Uri.EscapeDataString(expectedRawQuery);
        Assert.That(mockHandler.LastRequest, Is.Not.Null);
        
        // StringAssert.Contains -> Assert.That(..., Does.Contain(...)) 으로 변경
        Assert.That(mockHandler.LastRequest!.RequestUri!.Query, Does.Contain(expectedEncodedQuery));
    }
}