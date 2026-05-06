using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;

namespace RealTimePolling.Tests;

public class PollHubTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public PollHubTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task HubConnection_CanEstablish()
    {
        var server = _factory.Server;
        var hubConnection = new HubConnectionBuilder()
            .WithUrl(
                new Uri(server.BaseAddress, "/hubs/poll"),
                options => options.HttpMessageHandlerFactory = _ => server.CreateHandler())
            .Build();

        await hubConnection.StartAsync();

        Assert.Equal(HubConnectionState.Connected, hubConnection.State);

        await hubConnection.StopAsync();
    }

    [Fact]
    public async Task JoinRoom_SucceedsWithValidRoomCode()
    {
        var server = _factory.Server;
        var hubConnection = new HubConnectionBuilder()
            .WithUrl(
                new Uri(server.BaseAddress, "/hubs/poll"),
                options => options.HttpMessageHandlerFactory = _ => server.CreateHandler())
            .Build();

        await hubConnection.StartAsync();

        await hubConnection.InvokeAsync("JoinRoom", "1234");

        Assert.Equal(HubConnectionState.Connected, hubConnection.State);

        await hubConnection.StopAsync();
    }

    [Fact]
    public async Task MultipleClients_CanJoinSameRoom()
    {
        var server = _factory.Server;
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

        await connection1.StartAsync();
        await connection2.StartAsync();

        await connection1.InvokeAsync("JoinRoom", "5678");
        await connection2.InvokeAsync("JoinRoom", "5678");

        Assert.Equal(HubConnectionState.Connected, connection1.State);
        Assert.Equal(HubConnectionState.Connected, connection2.State);

        await connection1.StopAsync();
        await connection2.StopAsync();
    }
}
