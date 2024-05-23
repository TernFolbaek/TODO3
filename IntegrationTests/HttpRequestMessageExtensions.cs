using System.Net.Http.Json;

namespace IntegrationTests;

public static class HttpRequestMessageExtensions
{
    public static HttpRequestMessage WithJsonContent(this HttpRequestMessage request, object content)
    {
        ArgumentNullException.ThrowIfNull(request);
        
        request.Content = JsonContent.Create(content);
        return request;
    }
    
}