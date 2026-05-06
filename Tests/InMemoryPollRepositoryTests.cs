using _1_the_real_time_polling_app_focus_signalr_websockets.Models;
using _1_the_real_time_polling_app_focus_signalr_websockets.Repositories;

namespace RealTimePolling.Tests;

public class InMemoryPollRepositoryTests
{
    private InMemoryPollRepository CreateRepository() => new();

    private Poll CreateTestPoll(string roomCode = "1234")
    {
        return new Poll
        {
            Question = "What is your favorite color?",
            RoomCode = roomCode,
            Options = new List<PollOption>
            {
                new PollOption { Text = "Red" },
                new PollOption { Text = "Blue" },
                new PollOption { Text = "Green" }
            }
        };
    }

    #region CreatePoll Tests

    [Fact]
    public void CreatePoll_AssignsUniqueId()
    {
        var repository = CreateRepository();
        var poll = CreateTestPoll();

        var createdPoll = repository.CreatePoll(poll);

        Assert.True(createdPoll.Id > 0);
    }

    [Fact]
    public void CreatePoll_AssignsUniqueIdsToOptions()
    {
        var repository = CreateRepository();
        var poll = CreateTestPoll();

        var createdPoll = repository.CreatePoll(poll);

        Assert.All(createdPoll.Options, option => Assert.True(option.Id > 0));
        var optionIds = createdPoll.Options.Select(o => o.Id).ToList();
        Assert.Equal(optionIds.Count, optionIds.Distinct().Count());
    }

    [Fact]
    public void CreatePoll_SetsCorrectPollIdOnOptions()
    {
        var repository = CreateRepository();
        var poll = CreateTestPoll();

        var createdPoll = repository.CreatePoll(poll);

        Assert.All(createdPoll.Options, option => Assert.Equal(createdPoll.Id, option.PollId));
    }

    [Fact]
    public void CreatePoll_SetsPollReferenceOnOptions()
    {
        var repository = CreateRepository();
        var poll = CreateTestPoll();

        var createdPoll = repository.CreatePoll(poll);

        Assert.All(createdPoll.Options, option => Assert.Same(createdPoll, option.Poll));
    }

    [Fact]
    public void CreatePoll_ReturnsSamePollInstance()
    {
        var repository = CreateRepository();
        var poll = CreateTestPoll();

        var createdPoll = repository.CreatePoll(poll);

        Assert.Same(poll, createdPoll);
    }

    [Fact]
    public void CreatePoll_MultiplePolls_HaveUniqueIds()
    {
        var repository = CreateRepository();
        var poll1 = CreateTestPoll("1111");
        var poll2 = CreateTestPoll("2222");

        var created1 = repository.CreatePoll(poll1);
        var created2 = repository.CreatePoll(poll2);

        Assert.NotEqual(created1.Id, created2.Id);
    }

    [Fact]
    public void CreatePoll_DuplicateRoomCode_ThrowsException()
    {
        var repository = CreateRepository();
        var poll1 = CreateTestPoll("1234");
        var poll2 = CreateTestPoll("1234");

        repository.CreatePoll(poll1);

        Assert.Throws<InvalidOperationException>(() => repository.CreatePoll(poll2));
    }

    #endregion

    #region GetByRoomCode Tests

    [Fact]
    public void GetByRoomCode_ExistingPoll_ReturnsPoll()
    {
        var repository = CreateRepository();
        var poll = CreateTestPoll("5678");
        repository.CreatePoll(poll);

        var result = repository.GetByRoomCode("5678");

        Assert.NotNull(result);
        Assert.Equal("5678", result.RoomCode);
        Assert.Equal(poll.Question, result.Question);
    }

    [Fact]
    public void GetByRoomCode_NonExistingPoll_ReturnsNull()
    {
        var repository = CreateRepository();

        var result = repository.GetByRoomCode("9999");

        Assert.Null(result);
    }

    [Fact]
    public void GetByRoomCode_ReturnsSamePollInstance()
    {
        var repository = CreateRepository();
        var poll = CreateTestPoll();
        var created = repository.CreatePoll(poll);

        var result = repository.GetByRoomCode(poll.RoomCode);

        Assert.Same(created, result);
    }

    #endregion

    #region AddVote Tests

    [Fact]
    public void AddVote_ValidOption_ReturnsTrue()
    {
        var repository = CreateRepository();
        var poll = CreateTestPoll();
        repository.CreatePoll(poll);
        var optionId = poll.Options.First().Id;

        var result = repository.AddVote(poll.RoomCode, optionId);

        Assert.True(result);
    }

    [Fact]
    public void AddVote_ValidOption_IncrementsVoteCount()
    {
        var repository = CreateRepository();
        var poll = CreateTestPoll();
        repository.CreatePoll(poll);
        var option = poll.Options.First();

        repository.AddVote(poll.RoomCode, option.Id);

        Assert.Equal(1, option.VoteCount);
    }

    [Fact]
    public void AddVote_MultipleVotes_IncrementsCorrectly()
    {
        var repository = CreateRepository();
        var poll = CreateTestPoll();
        repository.CreatePoll(poll);
        var option = poll.Options.First();

        repository.AddVote(poll.RoomCode, option.Id);
        repository.AddVote(poll.RoomCode, option.Id);
        repository.AddVote(poll.RoomCode, option.Id);

        Assert.Equal(3, option.VoteCount);
    }

    [Fact]
    public void AddVote_NonExistingRoomCode_ReturnsFalse()
    {
        var repository = CreateRepository();

        var result = repository.AddVote("9999", 1);

        Assert.False(result);
    }

    [Fact]
    public void AddVote_NonExistingOptionId_ReturnsFalse()
    {
        var repository = CreateRepository();
        var poll = CreateTestPoll();
        repository.CreatePoll(poll);

        var result = repository.AddVote(poll.RoomCode, 99999);

        Assert.False(result);
    }

    [Fact]
    public void AddVote_InactivePoll_ReturnsFalse()
    {
        var repository = CreateRepository();
        var poll = CreateTestPoll();
        poll.IsActive = false;
        repository.CreatePoll(poll);
        var optionId = poll.Options.First().Id;

        var result = repository.AddVote(poll.RoomCode, optionId);

        Assert.False(result);
    }

    [Fact]
    public void AddVote_DifferentOptions_IncrementsIndependently()
    {
        var repository = CreateRepository();
        var poll = CreateTestPoll();
        repository.CreatePoll(poll);
        var options = poll.Options.ToList();

        repository.AddVote(poll.RoomCode, options[0].Id);
        repository.AddVote(poll.RoomCode, options[0].Id);
        repository.AddVote(poll.RoomCode, options[1].Id);

        Assert.Equal(2, options[0].VoteCount);
        Assert.Equal(1, options[1].VoteCount);
        Assert.Equal(0, options[2].VoteCount);
    }

    #endregion

    #region GetResults Tests

    [Fact]
    public void GetResults_ExistingPoll_ReturnsDictionary()
    {
        var repository = CreateRepository();
        var poll = CreateTestPoll();
        repository.CreatePoll(poll);

        var results = repository.GetResults(poll.RoomCode);

        Assert.NotNull(results);
        Assert.Equal(poll.Options.Count, results.Count);
    }

    [Fact]
    public void GetResults_NonExistingPoll_ReturnsNull()
    {
        var repository = CreateRepository();

        var results = repository.GetResults("9999");

        Assert.Null(results);
    }

    [Fact]
    public void GetResults_ReturnsCorrectVoteCounts()
    {
        var repository = CreateRepository();
        var poll = CreateTestPoll();
        repository.CreatePoll(poll);
        var options = poll.Options.ToList();

        repository.AddVote(poll.RoomCode, options[0].Id);
        repository.AddVote(poll.RoomCode, options[0].Id);
        repository.AddVote(poll.RoomCode, options[1].Id);

        var results = repository.GetResults(poll.RoomCode);

        Assert.NotNull(results);
        Assert.Equal(2, results[options[0].Id]);
        Assert.Equal(1, results[options[1].Id]);
        Assert.Equal(0, results[options[2].Id]);
    }

    [Fact]
    public void GetResults_InitiallyAllZero()
    {
        var repository = CreateRepository();
        var poll = CreateTestPoll();
        repository.CreatePoll(poll);

        var results = repository.GetResults(poll.RoomCode);

        Assert.NotNull(results);
        Assert.All(results.Values, count => Assert.Equal(0, count));
    }

    #endregion

    #region HasVoterVoted Tests

    [Fact]
    public void HasVoterVoted_NoVotes_ReturnsFalse()
    {
        var repository = CreateRepository();
        var poll = CreateTestPoll();
        repository.CreatePoll(poll);

        var result = repository.HasVoterVoted(poll.RoomCode, "voter-123");

        Assert.False(result);
    }

    [Fact]
    public void HasVoterVoted_AfterVote_ReturnsTrue()
    {
        var repository = CreateRepository();
        var poll = CreateTestPoll();
        repository.CreatePoll(poll);
        var optionId = poll.Options.First().Id;
        var voterId = "voter-123";

        repository.AddVote(poll.RoomCode, optionId, voterId);
        var result = repository.HasVoterVoted(poll.RoomCode, voterId);

        Assert.True(result);
    }

    [Fact]
    public void HasVoterVoted_DifferentVoter_ReturnsFalse()
    {
        var repository = CreateRepository();
        var poll = CreateTestPoll();
        repository.CreatePoll(poll);
        var optionId = poll.Options.First().Id;

        repository.AddVote(poll.RoomCode, optionId, "voter-123");
        var result = repository.HasVoterVoted(poll.RoomCode, "voter-456");

        Assert.False(result);
    }

    [Fact]
    public void HasVoterVoted_NonExistingPoll_ReturnsFalse()
    {
        var repository = CreateRepository();

        var result = repository.HasVoterVoted("9999", "voter-123");

        Assert.False(result);
    }

    [Fact]
    public void HasVoterVoted_NullVoterId_ReturnsFalse()
    {
        var repository = CreateRepository();
        var poll = CreateTestPoll();
        repository.CreatePoll(poll);

        var result = repository.HasVoterVoted(poll.RoomCode, null!);

        Assert.False(result);
    }

    [Fact]
    public void HasVoterVoted_EmptyVoterId_ReturnsFalse()
    {
        var repository = CreateRepository();
        var poll = CreateTestPoll();
        repository.CreatePoll(poll);

        var result = repository.HasVoterVoted(poll.RoomCode, "");

        Assert.False(result);
    }

    [Fact]
    public void AddVote_WithVoterId_TracksVoter()
    {
        var repository = CreateRepository();
        var poll = CreateTestPoll();
        repository.CreatePoll(poll);
        var optionId = poll.Options.First().Id;
        var voterId = "voter-abc";

        repository.AddVote(poll.RoomCode, optionId, voterId);

        Assert.True(repository.HasVoterVoted(poll.RoomCode, voterId));
    }

    [Fact]
    public void AddVote_WithoutVoterId_DoesNotTrackVoter()
    {
        var repository = CreateRepository();
        var poll = CreateTestPoll();
        repository.CreatePoll(poll);
        var optionId = poll.Options.First().Id;

        repository.AddVote(poll.RoomCode, optionId);

        Assert.False(repository.HasVoterVoted(poll.RoomCode, "any-voter"));
    }

    [Fact]
    public void HasVoterVoted_DifferentPolls_TrackedSeparately()
    {
        var repository = CreateRepository();
        var poll1 = CreateTestPoll("1111");
        var poll2 = CreateTestPoll("2222");
        repository.CreatePoll(poll1);
        repository.CreatePoll(poll2);
        var voterId = "voter-xyz";

        repository.AddVote(poll1.RoomCode, poll1.Options.First().Id, voterId);

        Assert.True(repository.HasVoterVoted(poll1.RoomCode, voterId));
        Assert.False(repository.HasVoterVoted(poll2.RoomCode, voterId));
    }

    #endregion

    #region Thread-Safety Tests

    [Fact]
    public async Task AddVote_ConcurrentVotes_CountsAllVotes()
    {
        var repository = CreateRepository();
        var poll = CreateTestPoll();
        repository.CreatePoll(poll);
        var option = poll.Options.First();
        var voteCount = 1000;

        var tasks = Enumerable.Range(0, voteCount)
            .Select(_ => Task.Run(() => repository.AddVote(poll.RoomCode, option.Id)))
            .ToArray();

        await Task.WhenAll(tasks);

        Assert.Equal(voteCount, option.VoteCount);
    }

    [Fact]
    public async Task AddVote_ConcurrentVotesOnMultipleOptions_CountsAllVotes()
    {
        var repository = CreateRepository();
        var poll = CreateTestPoll();
        repository.CreatePoll(poll);
        var options = poll.Options.ToList();
        var votesPerOption = 500;

        var tasks = new List<Task>();
        foreach (var option in options)
        {
            for (int i = 0; i < votesPerOption; i++)
            {
                var optionId = option.Id;
                tasks.Add(Task.Run(() => repository.AddVote(poll.RoomCode, optionId)));
            }
        }

        await Task.WhenAll(tasks.ToArray());

        Assert.All(options, option => Assert.Equal(votesPerOption, option.VoteCount));
    }

    [Fact]
    public async Task CreatePoll_ConcurrentCreation_AllSucceedWithUniqueIds()
    {
        var repository = CreateRepository();
        var pollCount = 100;
        var polls = Enumerable.Range(0, pollCount)
            .Select(i => CreateTestPoll(i.ToString("D4")))
            .ToList();

        var tasks = polls.Select(poll => Task.Run(() => repository.CreatePoll(poll))).ToArray();

        await Task.WhenAll(tasks);

        var allIds = polls.Select(p => p.Id).ToList();
        Assert.Equal(pollCount, allIds.Distinct().Count());
    }

    [Fact]
    public async Task GetByRoomCode_ConcurrentReads_AllReturnCorrectPoll()
    {
        var repository = CreateRepository();
        var poll = CreateTestPoll();
        repository.CreatePoll(poll);
        var readCount = 1000;

        var results = new Poll?[readCount];
        var tasks = Enumerable.Range(0, readCount)
            .Select(i => Task.Run(() => results[i] = repository.GetByRoomCode(poll.RoomCode)))
            .ToArray();

        await Task.WhenAll(tasks);

        Assert.All(results, result => Assert.Same(poll, result));
    }

    [Fact]
    public async Task ConcurrentOperations_MixedReadWriteVote_WorksCorrectly()
    {
        var repository = CreateRepository();
        var poll = CreateTestPoll();
        repository.CreatePoll(poll);
        var option = poll.Options.First();

        var tasks = new List<Task>();

        // Add votes
        for (int i = 0; i < 500; i++)
        {
            tasks.Add(Task.Run(() => repository.AddVote(poll.RoomCode, option.Id)));
        }

        // Read poll
        for (int i = 0; i < 100; i++)
        {
            tasks.Add(Task.Run(() => repository.GetByRoomCode(poll.RoomCode)));
        }

        // Get results
        for (int i = 0; i < 100; i++)
        {
            tasks.Add(Task.Run(() => repository.GetResults(poll.RoomCode)));
        }

        await Task.WhenAll(tasks.ToArray());

        Assert.Equal(500, option.VoteCount);
    }

    #endregion
}
