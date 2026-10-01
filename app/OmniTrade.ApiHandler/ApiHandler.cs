using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace OmniTrade.ApiHandler
{
    public class ApiHandler<T, T1> : IApiHandler<T, T1>
    {
        private readonly HttpClient _client;

        public ApiHandler(HttpClient? client = null)
        {
            _client = client ?? new HttpClient();
        }

        public async Task<T> HttpHandlerAsync(T1? requestBody, string url, HttpMethod method, Dictionary<string, string> headers, string methodType)
        {
            using var request = new HttpRequestMessage(method, url);
            request.Headers.Add("Accept", "application/json");

            foreach (var header in headers)
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            // Only attach body for methods that support it
            if ((method == HttpMethod.Post || method == HttpMethod.Put || method == HttpMethod.Patch)
                && requestBody != null)
            {
                var jsonBody = JsonConvert.SerializeObject(requestBody);
                request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            }

            var response = await _client.SendAsync(request);

            try
            {
                response.EnsureSuccessStatusCode();
                var responseContent = await response.Content.ReadAsStringAsync();

                return JsonConvert.DeserializeObject<T>(responseContent)!;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API Error - {methodType}] {ex}");

                var responseContent = await response.Content.ReadAsStringAsync();

                // Optionally throw instead of returning default
                throw new HttpRequestException(
                    $"Request failed ({methodType}): {responseContent}",
                    ex
                );
            }
        }
    }
}
