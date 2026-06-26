using UGIdotNET.SpikeTime.MutationTesting.Data;

namespace UGIdotNET.SpikeTime.MutationTesting.Test;

public class TodoItemTest
{
    [Fact]
    public void IsCompleted_Should_Be_False_When_CompletedAt_Is_Null()
    {
        var item = new TodoItem { Title = "x", CreatedAt = DateTime.Now };

        Assert.False(item.IsCompleted);
    }

    [Fact]
    public void IsCompleted_Should_Be_True_When_CompletedAt_Is_Set()
    {
        var item = new TodoItem { Title = "x", CreatedAt = DateTime.Now, CompletedAt = DateTime.Now };

        Assert.True(item.IsCompleted);
    }

    [Fact]
    public void MarkAsComplete_Should_Set_CompletedAt()
    {
        var item = new TodoItem { Title = "x", CreatedAt = DateTime.Now };

        item.MarkAsComplete();

        Assert.True(item.IsCompleted);
    }

    [Fact]
    public void IsOverdue_Should_Be_True_When_Pending_And_Older_Than_7_Days()
    {
        var now = new DateTime(2026, 06, 26);
        var item = new TodoItem { Title = "x", CreatedAt = now.AddDays(-8) };

        Assert.True(item.IsOverdue(now));
    }

    [Fact]
    public void IsOverdue_Should_Be_False_When_Pending_And_Exactly_7_Days_Old()
    {
        var now = new DateTime(2026, 06, 26);
        var item = new TodoItem { Title = "x", CreatedAt = now.AddDays(-7) };

        Assert.False(item.IsOverdue(now));
    }

    [Fact]
    public void IsOverdue_Should_Be_False_When_Pending_And_Younger_Than_7_Days()
    {
        var now = new DateTime(2026, 06, 26);
        var item = new TodoItem { Title = "x", CreatedAt = now.AddDays(-6) };

        Assert.False(item.IsOverdue(now));
    }

    [Fact]
    public void IsOverdue_Should_Be_False_When_Completed_Even_If_Old()
    {
        var now = new DateTime(2026, 06, 26);
        var item = new TodoItem { Title = "x", CreatedAt = now.AddDays(-30), CompletedAt = now };

        Assert.False(item.IsOverdue(now));
    }
}
