using System.ComponentModel.DataAnnotations;
using _1_the_real_time_polling_app_focus_signalr_websockets.Models;

namespace RealTimePolling.Tests;

public class PollOptionTests
{
    private static List<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(model, null, null);
        Validator.TryValidateObject(model, context, validationResults, true);
        return validationResults;
    }

    [Fact]
    public void PollOption_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var option = new PollOption();

        // Assert
        Assert.Equal(0, option.Id);
        Assert.Equal(0, option.PollId);
        Assert.Equal(string.Empty, option.Text);
        Assert.Equal(0, option.VoteCount);
        Assert.Null(option.Poll);
    }

    [Fact]
    public void PollOption_WithValidData_PassesValidation()
    {
        // Arrange
        var option = new PollOption
        {
            Id = 1,
            PollId = 1,
            Text = "Option A",
            VoteCount = 5
        };

        // Act
        var validationResults = ValidateModel(option);

        // Assert
        Assert.Empty(validationResults);
    }

    [Fact]
    public void PollOption_WithEmptyText_FailsValidation()
    {
        // Arrange
        var option = new PollOption
        {
            PollId = 1,
            Text = ""
        };

        // Act
        var validationResults = ValidateModel(option);

        // Assert
        Assert.Contains(validationResults, v => v.MemberNames.Contains("Text"));
    }

    [Fact]
    public void PollOption_TextTooLong_FailsValidation()
    {
        // Arrange
        var option = new PollOption
        {
            PollId = 1,
            Text = new string('a', 201)
        };

        // Act
        var validationResults = ValidateModel(option);

        // Assert
        Assert.Contains(validationResults, v => v.MemberNames.Contains("Text"));
    }

    [Fact]
    public void PollOption_WithMaxLengthText_PassesValidation()
    {
        // Arrange
        var option = new PollOption
        {
            PollId = 1,
            Text = new string('a', 200)
        };

        // Act
        var validationResults = ValidateModel(option);

        // Assert
        Assert.DoesNotContain(validationResults, v => v.MemberNames.Contains("Text"));
    }

    [Fact]
    public void PollOption_VoteCountDefaultsToZero()
    {
        // Arrange & Act
        var option = new PollOption
        {
            PollId = 1,
            Text = "Test Option"
        };

        // Assert
        Assert.Equal(0, option.VoteCount);
    }

    [Fact]
    public void PollOption_CanIncrementVoteCount()
    {
        // Arrange
        var option = new PollOption
        {
            PollId = 1,
            Text = "Test Option",
            VoteCount = 0
        };

        // Act
        option.VoteCount++;

        // Assert
        Assert.Equal(1, option.VoteCount);
    }

    [Fact]
    public void PollOption_CanBeAssociatedWithPoll()
    {
        // Arrange
        var poll = new Poll
        {
            Id = 1,
            Question = "Test question?",
            RoomCode = "1234"
        };

        var option = new PollOption
        {
            Id = 1,
            PollId = poll.Id,
            Text = "Option A",
            Poll = poll
        };

        // Act & Assert
        Assert.Equal(poll.Id, option.PollId);
        Assert.Same(poll, option.Poll);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(int.MaxValue)]
    public void PollOption_ValidVoteCounts_PassValidation(int voteCount)
    {
        // Arrange
        var option = new PollOption
        {
            PollId = 1,
            Text = "Test Option",
            VoteCount = voteCount
        };

        // Act
        var validationResults = ValidateModel(option);

        // Assert
        Assert.DoesNotContain(validationResults, v => v.MemberNames.Contains("VoteCount"));
    }
}
