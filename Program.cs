using _1_the_real_time_polling_app_focus_signalr_websockets.Dtos;
using _1_the_real_time_polling_app_focus_signalr_websockets.Models;
using _1_the_real_time_polling_app_focus_signalr_websockets.Repositories;
using _1_the_real_time_polling_app_focus_signalr_websockets.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Register the in-memory poll repository as singleton for thread-safe shared state
builder.Services.AddSingleton<IPollRepository, InMemoryPollRepository>();

// Register the room code generator
builder.Services.AddSingleton<IRoomCodeGenerator, RoomCodeGenerator>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// POST /api/polls - Create a new poll
app.MapPost("/api/polls", (CreatePollRequest request, IPollRepository repository, IRoomCodeGenerator codeGenerator) =>
{
    // Validate request
    var validationErrors = new List<string>();

    if (string.IsNullOrWhiteSpace(request.Question))
    {
        validationErrors.Add("Question is required");
    }

    if (request.Options == null || request.Options.Count < 2)
    {
        validationErrors.Add("At least 2 options are required");
    }
    else if (request.Options.Count > 4)
    {
        validationErrors.Add("Maximum 4 options are allowed");
    }
    else
    {
        // Check for empty option texts
        for (int i = 0; i < request.Options.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(request.Options[i]))
            {
                validationErrors.Add($"Option {i + 1} text cannot be empty");
            }
        }
    }

    if (validationErrors.Any())
    {
        return Results.BadRequest(new { errors = validationErrors });
    }

    // Generate unique room code
    var roomCode = codeGenerator.GenerateUniqueCode();

    // Create poll
    var poll = new Poll
    {
        Question = request.Question.Trim(),
        RoomCode = roomCode,
        Options = request.Options!.Select(optionText => new PollOption
        {
            Text = optionText.Trim()
        }).ToList()
    };

    var createdPoll = repository.CreatePoll(poll);

    // Build response
    var response = new CreatePollResponse
    {
        Id = createdPoll.Id,
        RoomCode = createdPoll.RoomCode,
        Question = createdPoll.Question,
        CreatedAt = createdPoll.CreatedAt,
        Options = createdPoll.Options.Select(o => new PollOptionResponse
        {
            Id = o.Id,
            Text = o.Text,
            VoteCount = o.VoteCount
        }).ToList()
    };

    return Results.Created($"/api/polls/{createdPoll.RoomCode}", response);
})
.WithName("CreatePoll")
.WithDescription("Create a new poll with a question and 2-4 options");

// GET /api/polls/{roomCode} - Get poll for voters (without vote counts)
app.MapGet("/api/polls/{roomCode}", (string roomCode, IPollRepository repository) =>
{
    var poll = repository.GetByRoomCode(roomCode);
    if (poll == null)
    {
        return Results.NotFound(new { error = "Poll not found" });
    }

    var response = new GetPollResponse
    {
        Question = poll.Question,
        RoomCode = poll.RoomCode,
        Options = poll.Options.Select(o => new VoterPollOptionResponse
        {
            Id = o.Id,
            Text = o.Text
        }).ToList()
    };

    return Results.Ok(response);
})
.WithName("GetPoll")
.WithDescription("Get a poll by room code for voters");

// POST /api/polls/{roomCode}/vote - Cast a vote
app.MapPost("/api/polls/{roomCode}/vote", (string roomCode, VoteRequest request, IPollRepository repository) =>
{
    var poll = repository.GetByRoomCode(roomCode);
    if (poll == null)
    {
        return Results.NotFound(new { error = "Poll not found" });
    }

    var option = poll.Options.FirstOrDefault(o => o.Id == request.OptionId);
    if (option == null)
    {
        return Results.BadRequest(new { error = "Invalid option ID" });
    }

    var success = repository.AddVote(roomCode, request.OptionId);
    if (!success)
    {
        return Results.BadRequest(new { error = "Unable to record vote" });
    }

    return Results.Ok(new { message = "Vote recorded successfully" });
})
.WithName("CastVote")
.WithDescription("Cast a vote for a poll option");

app.Run();

// Make Program class accessible for integration tests
public partial class Program { }
