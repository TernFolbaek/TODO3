using FluentAssertions;
using TODO.Models;

namespace IntegrationTests;

public class ExampleUnitTests
{
    [Fact]
    public void TodoItem_SetsAndGetWorks()
    {
        var todoItem = new TodoItem()
        {
            Id = 123,
            Description = "Hej",
            IsComplete = true,
            DueDate = DateTime.Now
        };

        todoItem.Id.Should().Be(123);
        todoItem.Description.Should().Be("Hej");
    }

    [Fact]
    public void CreateUserWorks()
    {
        var user = new User()
        {
            Id = 1,
            Username = "Justin",
            Password = "root"
        };

        user.Id.Should().Be(1);
        user.Username.Should().Be("Justin");
        user.Password.Should().Be("root");

    }
    
}