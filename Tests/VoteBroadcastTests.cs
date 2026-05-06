using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using _1_the_real_time_polling_app_focus_signalr_websockets.Dtos;

namespace RealTimePolling.Tests;

public class VoteBroadcastTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public VoteBroadcastTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private async Task<CreatePollResponse> CreatePollAsync(HttpClient client)
    {
        var request = new CreatePollRequest
        {
            Question = "Broadcast test question?",
            Options = new List<string> { "Option A", "Option B", "Option C" }
        };
        var response = await client.PostAsJsonAsync("/api/polls", request);
        return (await response.Content.ReadFromJsonAsync<CreatePollResponse>())!;
    }

    [Fact]
    public async Task Vote_BroadcastsVoteUpdatedToGroupMembers()
    {
        var server = _factory.Server;
        var client = _factory.CreateClient();
        var poll = await CreatePollAsync(client);

        var hubConnection = new HubConnectionBuilder()
            .WithUrl(
                new Uri(server.BaseAddress, "/hubs/poll"),
                options => options.HttpMessageHandlerFactory = _ => server.CreateHandler())
            .Build();

        VoteUpdatePayload? receivedPayload = null;
        var messageReceived = new TaskCompletionSource<bool>();

        hubConnection.On<VoteUpdatePayload>("VoteUpdated", payload =>
        {
            receivedPayload = payload;
            messageReceived.TrySetResult(true);
        });

        await hubConnection.StartAsync();
        await hubConnection.InvokeAsync("JoinRoom", poll.RoomCode);

        var voteRequest = new VoteRequest { OptionId = poll.Options[0].Id };
        await client.PostAsJsonAsync($"/api/polls/{poll.RoomCode}/vote", voteRequest);

        var completed = await Task.WhenAny(messageReceived.Task, Task.Delay(5000));

        Assert.True(completed == messageReceived.Task, "Did not receive VoteUpdated message within timeout");
        Assert.NotNull(receivedPayload);
        Assert.Equal(poll.RoomCode, receivedPayload.RoomCode);

        await hubConnection.StopAsync();
    }

    [Fact]
    public async Task Vote_BroadcastContainsAllOptionVoteCounts()
    {
        var server = _factory.Server;
        var client = _factory.CreateClient();
        var poll = await CreatePollAsync(client);

        var hubConnection = new HubConnectionBuilder()
            .WithUrl(
                new Uri(server.BaseAddress, "/hubs/poll"),
                options => options.HttpMessageHandlerFactory = _ => server.CreateHandler())
            .Build();

        VoteUpdatePayload? receivedPayload = null;
        var messageReceived = new TaskCompletionSource<bool>();

        hubConnection.On<VoteUpdatePayload>("VoteUpdated", payload =>
        {
            receivedPayload = payload;
            messageReceived.TrySetResult(true);
        });

        await hubConnection.StartAsync();
        await hubConnection.InvokeAsync("JoinRoom", poll.RoomCode);

        var voteRequest = new VoteRequest { OptionId = poll.Options[0].Id };
        await client.PostAsJsonAsync($"/api/polls/{poll.RoomCode}/vote", voteRequest);

        await Task.WhenAny(messageReceived.Task, Task.Delay(5000));

        Assert.NotNull(receivedPayload);
        Assert.Equal(3, receivedPayload.Results.Count);

        var votedOption = receivedPayload.Results.First(r => r.OptionId == poll.Options[0].Id);
        Assert.Equal(1, votedOption.VoteCount);
        Assert.Equal("Option A", votedOption.Text);

        var otherOptions = receivedPayload.Results.Where(r => r.OptionId != poll.Options[0].Id).ToList();
        Assert.All(otherOptions, opt => Assert.Equal(0, opt.VoteCount));

        await hubConnection.StopAsync();
    }

    [Fact]
    public async Task Vote_MultipleVotes_BroadcastsUpdatedCounts()
    {
        var server = _factory.Server;
        var client = _factory.CreateClient();
        var poll = await CreatePollAsync(client);

        var hubConnection = new HubConnectionBuilder()
            .WithUrl(
                new Uri(server.BaseAddress, "/hubs/poll"),
                options => options.HttpMessageHandlerFactory = _ => server.CreateHandler())
            .Build();

        var receivedPayloads = new List<VoteUpdatePayload>();
        var messageCount = 0;
        var threeMessagesReceived = new TaskCompletionSource<bool>();

        hubConnection.On<VoteUpdatePayload>("VoteUpdated", payload =>
        {
            receivedPayloads.Add(payload);
            messageCount++;
            if (messageCount >= 3)
            {
                threeMessagesReceived.TrySetResult(true);
            }
        });

        await hubConnection.StartAsync();
        await hubConnection.InvokeAsync("JoinRoom", poll.RoomCode);

        await client.PostAsJsonAsync($"/api/polls/{poll.RoomCode}/vote", new VoteRequest { OptionId = poll.Options[0].Id });
        await client.PostAsJsonAsync($"/api/polls/{poll.RoomCode}/vote", new VoteRequest { OptionId = poll.Options[0].Id });
        await client.PostAsJsonAsync($"/api/polls/{poll.RoomCode}/vote", new VoteRequest { OptionId = poll.Options[1].Id });

        await Task.WhenAny(threeMessagesReceived.Task, Task.Delay(5000));

        Assert.Equal(3, receivedPayloads.Count);

        var lastPayload = receivedPayloads.Last();
        var option1Count = lastPayload.Results.First(r => r.OptionId == poll.Options[0].Id).VoteCount;
        var option2Count = lastPayload.Results.First(r => r.OptionId == poll.Options[1].Id).VoteCount;

        Assert.Equal(2, option1Count);
        Assert.Equal(1, option2Count);

        await hubConnection.StopAsync();
    }

    [Fact]
    public async Task Vote_OnlyBroadcastsToClientsInSameRoom()
    {
        var server = _factory.Server;
        var client = _factory.CreateClient();

        var poll1 = await CreatePollAsync(client);
        var poll2 = await CreatePollAsync(client);

        var connection1 = new HubConnectionBuilder()
            .WithUrl(
                new Uri(server.BaseAddress, "/hubs/poll"),
                options => options.HttpMessageHandlerFactory = _ => server.CreateHandler())
            .Build();

        var connection2 = new HubConnectionBuilder()
            .WithUrl(
                new Uri(server.BaseAddress, "/hubs/poll"),
                options => options.HttpMessageHandlerFactory = _ => server.CreateHandler())
            .Build();

        var room1Received = false;
        var room2Received = false;
        var room1Message = new TaskCompletionSource<bool>();

        connection1.On<VoteUpdatePayload>("VoteUpdated", _ =>
        {
            room1Received = true;
            room1Message.TrySetResult(true);
        });

        connection2.On<VoteUpdatePayload>("VoteUpdated", _ =>
        {
            room2Received = true;
        });

        await connection1.StartAsync();
        await connection2.StartAsync();

        await connection1.InvokeAsync("JoinRoom", poll1.RoomCode);
        await connection2.InvokeAsync("JoinRoom", poll2.RoomCode);

        await client.PostAsJsonAsync($"/api/polls/{poll1.RoomCode}/vote", new VoteRequest { OptionId = poll1.Options[0].Id });

        await Task.WhenAny(room1Message.Task, Task.Delay(2000));
        await Task.Delay(500);

        Assert.True(room1Received, "Client in room 1 should have received the update");
        Assert.False(room2Received, "Client in room 2 should NOT have received the update");

        await connection1.StopAsync();
        await connection2.StopAsync();
    }

    [Fact]
    public async Task Vote_MultipleClientsInSameRoom_AllReceiveUpdate()
    {
        var server = _factory.Server;
        var client = _factory.CreateClient();
        var poll = await CreatePollAsync(client);

        var connection1 = new HubConnectionBuilder()
            .WithUrl(
                new Uri(server.BaseAddress, "/hubs/poll"),
                options => options.HttpMessageHandlerFactory = _ => server.CreateHandler())
            .Build();

        var connection2 = new HubConnectionBuilder()
            .WithUrl(
                new Uri(server.BaseAddress, "/hubs/poll"),
                options => options.HttpMessageHandlerFactory = _ => server.CreateHandler())
            .Build();

        var client1Received = new TaskCompletionSource<bool>();
        var client2Received = new TaskCompletionSource<bool>();

        connection1.On<VoteUpdatePayload>("VoteUpdated", _ => client1Received.TrySetResult(true));
        connection2.On<VoteUpdatePayload>("VoteUpdated", _ => client2Received.TrySetResult(true));

        await connection1.StartAsync();
        await connection2.StartAsync();

        await connection1.InvokeAsync("JoinRoom", poll.RoomCode);
        await connection2.InvokeAsync("JoinRoom", poll.RoomCode);

        await client.PostAsJsonAsync($"/api/polls/{poll.RoomCode}/vote", new VoteRequest { OptionId = poll.Options[0].Id });

        var bothReceived = await Task.WhenAll(
            Task.WhenAny(client1Received.Task, Task.Delay(5000)),
            Task.WhenAny(client2Received.Task, Task.Delay(5000))
        );

        Assert.True(client1Received.Task.IsCompleted && client1Received.Task.Result, "Client 1 should have received the update");
        Assert.True(client2Received.Task.IsCompleted && client2Received.Task.Result, "Client 2 should have received the update");

        await connection1.StopAsync();
        await connection2.StopAsync();
    }
}
