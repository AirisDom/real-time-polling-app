using _1_the_real_time_polling_app_focus_signalr_websockets.Models;

namespace _1_the_real_time_polling_app_focus_signalr_websockets.Repositories;

public interface IPollRepository
{
    /// <summary>
    /// Creates a new poll and stores it in the repository.
    /// </summary>
    /// <param name="poll">The poll to create.</param>
    /// <returns>The created poll with assigned ID.</returns>
    Poll CreatePoll(Poll poll);

    /// <summary>
    /// Retrieves a poll by its unique room code.
    /// </summary>
    /// <param name="roomCode">The 4-digit room code.</param>
    /// <returns>The poll if found, otherwise null.</returns>
    Poll? GetByRoomCode(string roomCode);

    /// <summary>
    /// Adds a vote to a specific option within a poll.
    /// </summary>
    /// <param name="roomCode">The room code of the poll.</param>
    /// <param name="optionId">The ID of the option to vote for.</param>
    /// <param name="voterId">Optional voter ID for deduplication.</param>
    /// <returns>True if vote succeeded, false if poll/option not found.</returns>
    bool AddVote(string roomCode, int optionId, string? voterId = null);

    /// <summary>
    /// Checks if a voter has already voted in a poll.
    /// </summary>
    bool HasVoterVoted(string roomCode, string voterId);

    /// <summary>
    /// Gets the current results (vote counts) for a poll.
    /// </summary>
    /// <param name="roomCode">The room code of the poll.</param>
    /// <returns>A dictionary mapping option IDs to their vote counts, or null if poll not found.</returns>
    Dictionary<int, int>? GetResults(string roomCode);
}
