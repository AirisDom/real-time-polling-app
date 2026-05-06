using System.ComponentModel.DataAnnotations;

namespace _1_the_real_time_polling_app_focus_signalr_websockets.Dtos;

public class VoteRequest
{
    [Required(ErrorMessage = "Option ID is required")]
    public int OptionId { get; set; }

    public string? VoterId { get; set; }
}
