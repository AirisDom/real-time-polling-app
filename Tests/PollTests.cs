using System.ComponentModel.DataAnnotations;
using _1_the_real_time_polling_app_focus_signalr_websockets.Models;

namespace RealTimePolling.Tests;

public class PollTests
{
    private static List<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(model, null, null);
        Validator.TryValidateObject(model, context, validationResults, true);
        return validationResults;
    }

    [Fact]
    public void Poll_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var poll = new Poll();

        // Assert
        Assert.Equal(0, poll.Id);
        Assert.Equal(string.Empty, poll.Question);
        Assert.Equal(string.Empty, poll.RoomCode);
        Assert.True(poll.IsActive);
        Assert.NotNull(poll.Options);
        Assert.Empty(poll.Options);
        Assert.True(poll.CreatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void Poll_WithValidData_PassesValidation()
    {
        // Arrange
        var poll = new Poll
        {
            Id = 1,
            Question = "What is your favorite color?",
            RoomCode = "1234",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        // Act
        var validationResults = ValidateModel(poll);

        // Assert
        Assert.Empty(validationResults);
    }

    [Fact]
    public void Poll_WithEmptyQuestion_FailsValidation()
    {
        // Arrange
        var poll = new Poll
        {
            Question = "",
            RoomCode = "1234"
        };

        // Act
        var validationResults = ValidateModel(poll);

        // Assert
        Assert.Contains(validationResults, v => v.MemberNames.Contains("Question"));
    }

    [Fact]
    public void Poll_WithEmptyRoomCode_FailsValidation()
    {
        // Arrange
        var poll = new Poll
        {
            Question = "Test question?",
            RoomCode = ""
        };

        // Act
        var validationResults = ValidateModel(poll);

        // Assert
        Assert.Contains(validationResults, v => v.MemberNames.Contains("RoomCode"));
    }

    [Theory]
    [InlineData("123")]
    [InlineData("12345")]
    [InlineData("abcd")]
    [InlineData("12ab")]
    public void Poll_WithInvalidRoomCode_FailsValidation(string roomCode)
    {
        // Arrange
        var poll = new Poll
        {
            Question = "Test question?",
            RoomCode = roomCode
        };

        // Act
        var validationResults = ValidateModel(poll);

        // Assert
        Assert.Contains(validationResults, v => v.MemberNames.Contains("RoomCode"));
    }

    [Theory]
    [InlineData("0000")]
    [InlineData("1234")]
    [InlineData("9999")]
    public void Poll_WithValidRoomCode_PassesValidation(string roomCode)
    {
        // Arrange
        var poll = new Poll
        {
            Question = "Test question?",
            RoomCode = roomCode
        };

        // Act
        var validationResults = ValidateModel(poll);

        // Assert
        Assert.DoesNotContain(validationResults, v => v.MemberNames.Contains("RoomCode"));
    }

    [Fact]
    public void Poll_QuestionTooLong_FailsValidation()
    {
        // Arrange
        var poll = new Poll
        {
            Question = new string('a', 501),
            RoomCode = "1234"
        };

        // Act
        var validationResults = ValidateModel(poll);

        // Assert
        Assert.Contains(validationResults, v => v.MemberNames.Contains("Question"));
    }

    [Fact]
    public void Poll_CanAddOptions()
    {
        // Arrange
        var poll = new Poll
        {
            Id = 1,
            Question = "What is your favorite color?",
            RoomCode = "1234"
        };

        var option1 = new PollOption { Id = 1, PollId = 1, Text = "Red" };
        var option2 = new PollOption { Id = 2, PollId = 1, Text = "Blue" };

        // Act
        poll.Options.Add(option1);
        poll.Options.Add(option2);

        // Assert
        Assert.Equal(2, poll.Options.Count);
    }
}
