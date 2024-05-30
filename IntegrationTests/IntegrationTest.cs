using AutoFixture;
using FluentAssertions;

namespace IntegrationTests;

public class IntegrationTest : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;

    public IntegrationTest(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }


    [Fact]
    public async Task TestTodo()
    {
        
        // Arrange
        using var client = _fixture.CreateDefaultClient();
        var fixture = new Fixture();
        using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, "api/Todo")
            .WithJsonContent(new
            {
                Id = fixture.Create<int>(),
                Description = "south america is the second largest continent in the world",
                IsComplete = false,
                DueDate = DateTime.UtcNow
            });

        // Act
        using var response = await client.SendAsync(httpRequestMessage);
        
        // Assert
        response.Should().BeSuccessful();
    }

    [Fact]
    public async Task TestSignup()
    {
        using var client = _fixture.CreateDefaultClient();
        var fixture = new Fixture();
        using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, "signup").WithJsonContent(
            new
            {
                Username = fixture.Create<string>().Substring(0, 10), 
                Password = "Yap2023!"  
            });

        using var response = await client.SendAsync(httpRequestMessage);


        if (!response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Expected successful response but got {response.StatusCode}: {responseContent}");
        }

        response.Should().BeSuccessful();
    }

    [Fact]
    public async Task TestLogin()
    {
        using var client = _fixture.CreateDefaultClient();

        using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, "login").WithJsonContent(
            new
            {
                Username = "root",
                Password = "root"
            });

        using var response = await client.SendAsync(httpRequestMessage);

        if (!response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Expected successful response but got {response.StatusCode}: {responseContent}");
        }
        
        response.Should().BeSuccessful();
        
    }
    



}