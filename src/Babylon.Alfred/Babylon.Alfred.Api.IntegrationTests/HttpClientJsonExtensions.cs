using System.Text;
using Newtonsoft.Json;

namespace Babylon.Alfred.Api.IntegrationTests;

/// <summary>
/// The Api serializes with Newtonsoft.Json (AddNewtonsoftJson), not System.Text.Json — use these
/// instead of the built-in System.Net.Http.Json helpers so request/response shapes match the real wire format.
/// </summary>
public static class HttpClientJsonExtensions
{
    public static Task<HttpResponseMessage> PostJsonAsync<T>(this HttpClient client, string requestUri, T body)
    {
        var json = JsonConvert.SerializeObject(body);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return client.PostAsync(requestUri, content);
    }

    public static async Task<T?> ReadJsonAsync<T>(this HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<T>(json);
    }
}
