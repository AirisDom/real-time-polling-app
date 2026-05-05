namespace _1_the_real_time_polling_app_focus_signalr_websockets.Dtos;

public class GetPollResponse
{
    public string Question { get; set; } = string.Empty;
    public string RoomCode { get; set; } = string.Empty;
    public List<VoterPollOptionResponse> Options { get; set; } = new();
}

public class VoterPollOptionResponse
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
}
