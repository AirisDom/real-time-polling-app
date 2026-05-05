using System.ComponentModel.DataAnnotations;

namespace _1_the_real_time_polling_app_focus_signalr_websockets.Models;

public class PollOption
{
    public int Id { get; set; }

    [Required]
    public int PollId { get; set; }

    [Required(ErrorMessage = "Option text is required")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Option text must be between 1 and 200 characters")]
    public string Text { get; set; } = string.Empty;

    [Range(0, int.MaxValue, ErrorMessage = "Vote count cannot be negative")]
    public int VoteCount { get; set; } = 0;

    public Poll? Poll { get; set; }
}
