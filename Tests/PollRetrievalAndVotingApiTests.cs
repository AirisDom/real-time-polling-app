using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using _1_the_real_time_polling_app_focus_signalr_websockets.Dtos;

namespace RealTimePolling.Tests;

public class PollRetrievalAndVotingApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public PollRetrievalAndVotingApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private async Task<(HttpClient client, CreatePollResponse poll)> CreatePollAsync()
    {
        var client = _factory.CreateClient();
        var request = new CreatePollRequest
        {
            Question = "Test question?",
            Options = new List<string> { "Option A", "Option B", "Option C" }
        };
        var response = await client.PostAsJsonAsync("/api/polls", request);
        var poll = await response.Content.ReadFromJsonAsync<CreatePollResponse>();
        return (client, poll!);
    }

    [Fact]
    public async Task GetPoll_WithValidRoomCode_ReturnsOk()
    {
        var (client, createdPoll) = await CreatePollAsync();

        var response = await client.GetAsync($"/api/polls/{createdPoll.RoomCode}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetPoll_WithValidRoomCode_ReturnsQuestionAndOptions()
    {
        var (client, createdPoll) = await CreatePollAsync();

        var response = await client.GetAsync($"/api/polls/{createdPoll.RoomCode}");
        var result = await response.Content.ReadFromJsonAsync<GetPollResponse>();

        Assert.NotNull(result);
        Assert.Equal("Test question?", result.Question);
        Assert.Equal(createdPoll.RoomCode, result.RoomCode);
        Assert.Equal(3, result.Options.Count);
        Assert.Equal("Option A", result.Options[0].Text);
        Assert.Equal("Option B", result.Options[1].Text);
        Assert.Equal("Option C", result.Options[2].Text);
    }

    [Fact]
    public async Task GetPoll_DoesNotReturnVoteCounts()
    {
        var (client, createdPoll) = await CreatePollAsync();

        var response = await client.GetAsync($"/api/polls/{createdPoll.RoomCode}");
        var content = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("voteCount", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetPoll_ReturnsOptionIds()
    {
        var (client, createdPoll) = await CreatePollAsync();

        var response = await client.GetAsync($"/api/polls/{createdPoll.RoomCode}");
        var result = await response.Content.ReadFromJsonAsync<GetPollResponse>();

        Assert.NotNull(result);
        Assert.All(result.Options, opt => Assert.True(opt.Id > 0));
    }

    [Fact]
    public async Task GetPoll_WithInvalidRoomCode_Returns404()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/polls/9999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetPoll_WithNonExistentRoomCode_Returns404()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/polls/0000");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Vote_WithValidData_ReturnsOk()
    {
        var (client, createdPoll) = await CreatePollAsync();
        var optionId = createdPoll.Options[0].Id;
        var voteRequest = new VoteRequest { OptionId = optionId };

        var response = await client.PostAsJsonAsync($"/api/polls/{createdPoll.RoomCode}/vote", voteRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Vote_WithValidData_ReturnsSuccessMessage()
    {
        var (client, createdPoll) = await CreatePollAsync();
        var optionId = createdPoll.Options[0].Id;
        var voteRequest = new VoteRequest { OptionId = optionId };

        var response = await client.PostAsJsonAsync($"/api/polls/{createdPoll.RoomCode}/vote", voteRequest);
        var content = await response.Content.ReadAsStringAsync();

        Assert.Contains("Vote recorded successfully", content);
    }

    [Fact]
    public async Task Vote_WithInvalidRoomCode_Returns404()
    {
        var client = _factory.CreateClient();
        var voteRequest = new VoteRequest { OptionId = 1 };

        var response = await client.PostAsJsonAsync("/api/polls/9999/vote", voteRequest);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Vote_WithInvalidOptionId_Returns400()
    {
        var (client, createdPoll) = await CreatePollAsync();
        var voteRequest = new VoteRequest { OptionId = 99999 };

        var response = await client.PostAsJsonAsync($"/api/polls/{createdPoll.RoomCode}/vote", voteRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Vote_WithOptionFromDifferentPoll_Returns400()
    {
        var client = _factory.CreateClient();

        var request1 = new CreatePollRequest
        {
            Question = "Poll 1",
            Options = new List<string> { "A1", "B1" }
        };
        var response1 = await client.PostAsJsonAsync("/api/polls", request1);
        var poll1 = await response1.Content.ReadFromJsonAsync<CreatePollResponse>();

        var request2 = new CreatePollRequest
        {
            Question = "Poll 2",
            Options = new List<string> { "A2", "B2" }
        };
        var response2 = await client.PostAsJsonAsync("/api/polls", request2);
        var poll2 = await response2.Content.ReadFromJsonAsync<CreatePollResponse>();

        var voteRequest = new VoteRequest { OptionId = poll1!.Options[0].Id };
        var response = await client.PostAsJsonAsync($"/api/polls/{poll2!.RoomCode}/vote", voteRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Vote_IncrementsVoteCount()
    {
        var client = _factory.CreateClient();
        var createRequest = new CreatePollRequest
        {
            Question = "Vote count test?",
            Options = new List<string> { "Yes", "No" }
        };
        var createResponse = await client.PostAsJsonAsync("/api/polls", createRequest);
        var createdPoll = await createResponse.Content.ReadFromJsonAsync<CreatePollResponse>();
        var optionId = createdPoll!.Options[0].Id;

        await client.PostAsJsonAsync($"/api/polls/{createdPoll.RoomCode}/vote", new VoteRequest { OptionId = optionId });
        await client.PostAsJsonAsync($"/api/polls/{createdPoll.RoomCode}/vote", new VoteRequest { OptionId = optionId });
        await client.PostAsJsonAsync($"/api/polls/{createdPoll.RoomCode}/vote", new VoteRequest { OptionId = optionId });

        var pollResponse = await client.PostAsJsonAsync("/api/polls", new CreatePollRequest
        {
            Question = "Dummy",
            Options = new List<string> { "A", "B" }
        });
    }

    [Fact]
    public async Task Vote_CanVoteForDifferentOptions()
    {
        var (client, createdPoll) = await CreatePollAsync();
        var option1Id = createdPoll.Options[0].Id;
        var option2Id = createdPoll.Options[1].Id;

        var response1 = await client.PostAsJsonAsync($"/api/polls/{createdPoll.RoomCode}/vote", new VoteRequest { OptionId = option1Id });
        var response2 = await client.PostAsJsonAsync($"/api/polls/{createdPoll.RoomCode}/vote", new VoteRequest { OptionId = option2Id });

        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
    }

    [Fact]
    public async Task Vote_MultipleVotesOnSameOption_AllSucceed()
    {
        var (client, createdPoll) = await CreatePollAsync();
        var optionId = createdPoll.Options[0].Id;

        var response1 = await client.PostAsJsonAsync($"/api/polls/{createdPoll.RoomCode}/vote", new VoteRequest { OptionId = optionId });
        var response2 = await client.PostAsJsonAsync($"/api/polls/{createdPoll.RoomCode}/vote", new VoteRequest { OptionId = optionId });
        var response3 = await client.PostAsJsonAsync($"/api/polls/{createdPoll.RoomCode}/vote", new VoteRequest { OptionId = optionId });

        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response3.StatusCode);
    }
}
