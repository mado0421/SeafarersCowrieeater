// See https://aka.ms/new-console-template for more information

using SeafarersCowrieeater;

TweetGetter tweetGetter = new TweetGetter();
DiscordMessageSender discordMessageSender = new DiscordMessageSender();

string twitterToken = "";
string discordToken = "";
string twitterId = "";
List<string> keywords = [""];

var tweetData = await tweetGetter.GetTweet(twitterToken, twitterId, keywords);
await discordMessageSender.SendAsync(discordToken, tweetData.Text, tweetData.Image);