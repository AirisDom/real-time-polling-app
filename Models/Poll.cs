using System.ComponentModel.DataAnnotations;

namespace _1_the_real_time_polling_app_focus_signalr_websockets.Models;

public class Poll
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Question is required")]
    [StringLength(500, MinimumLength = 1, ErrorMessage = "Question must be between 1 and 500 characters")]
    public string Question { get; set; } = string.Empty;

    [Required(ErrorMessage = "Room code is required")]
    [RegularExpression(@"^\d{4}$", ErrorMessage = "Room code must be exactly 4 digits")]
    public string RoomCode { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    public ICollection<PollOption> Options { get; set; } = new List<PollOption>();
}
