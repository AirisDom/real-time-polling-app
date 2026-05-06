namespace _1_the_real_time_polling_app_focus_signalr_websockets.Dtos;

public class VoteUpdatePayload
{
    public string RoomCode { get; set; } = string.Empty;
    public List<OptionVoteCount> Results { get; set; } = new();
}

public class OptionVoteCount
{
    public int OptionId { get; set; }
    public string Text { get; set; } = string.Empty;
    public int VoteCount { get; set; }
}
