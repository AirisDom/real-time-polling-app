using System.Collections.Concurrent;
using _1_the_real_time_polling_app_focus_signalr_websockets.Models;

namespace _1_the_real_time_polling_app_focus_signalr_websockets.Repositories;

public class InMemoryPollRepository : IPollRepository
{
    private readonly ConcurrentDictionary<string, Poll> _polls = new();
    private int _nextPollId = 1;
    private int _nextOptionId = 1;
    private readonly object _idLock = new();

    public Poll CreatePoll(Poll poll)
    {
        lock (_idLock)
        {
            poll.Id = _nextPollId++;

            foreach (var option in poll.Options)
            {
                option.Id = _nextOptionId++;
                option.PollId = poll.Id;
                option.Poll = poll;
            }
        }

        if (!_polls.TryAdd(poll.RoomCode, poll))
        {
            throw new InvalidOperationException($"A poll with room code '{poll.RoomCode}' already exists.");
        }

        return poll;
    }

    public Poll? GetByRoomCode(string roomCode)
    {
        _polls.TryGetValue(roomCode, out var poll);
        return poll;
    }

    public bool AddVote(string roomCode, int optionId)
    {
        if (!_polls.TryGetValue(roomCode, out var poll))
        {
            return false;
        }

        if (!poll.IsActive)
        {
            return false;
        }

        var option = poll.Options.FirstOrDefault(o => o.Id == optionId);
        if (option == null)
        {
            return false;
        }

        // Thread-safe increment using Interlocked
        Interlocked.Increment(ref option._voteCount);
        return true;
    }

    public Dictionary<int, int>? GetResults(string roomCode)
    {
        if (!_polls.TryGetValue(roomCode, out var poll))
        {
            return null;
        }

        return poll.Options.ToDictionary(o => o.Id, o => o.VoteCount);
    }
}
