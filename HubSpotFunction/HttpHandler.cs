using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace MAPHubSpotFunction
{
    public class HttpHandler
    {
        private static readonly string appVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();

        private readonly ILogger<HttpHandler> _logger;
        private readonly HttpClient httpClient;

        public HttpHandler(ILogger<HttpHandler> logger, HttpClient httpClient)
        {
            _logger = logger;
            this.httpClient = httpClient;
        }

        public async Task<JObject> PostRequestAsync(string endpoint, JObject payload)
        {
            _logger.LogInformation($"{appVersion}::Initiating POST request to {endpoint}.");
            var response = await httpClient.PostAsync(endpoint, new StringContent(payload.ToString(), System.Text.Encoding.UTF8, "application/json"));
            string responseBody = await response.Content.ReadAsStringAsync();
            _logger.LogInformation($"{appVersion}:: LN239{responseBody}");

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation($"{appVersion}::POST request to {endpoint} succeeded.");
                return JObject.Parse(responseBody);
            }
            else
            {
                _logger.LogError($"{appVersion}::POST request to {endpoint} failed: {response.StatusCode}, {responseBody}");
                throw new HttpRequestException($"POST request to {endpoint} failed with status code {response.StatusCode}.");
            }
        }
        public async Task<HttpResponseMessage> PatchRequestAsync(string endpoint, JObject payload)
        {
            // Log the endpoint and sanitized payload. Ensure no sensitive information is logged.
            _logger.LogInformation($"{appVersion}::Initiating PATCH request to {endpoint}.");
            _logger.LogInformation($"{appVersion}::Request payload: {payload}");

            var request = new HttpRequestMessage(HttpMethod.Patch, endpoint)
            {
                Content = new StringContent(payload.ToString(), System.Text.Encoding.UTF8, "application/json")
            };

            HttpResponseMessage response = null;
            try
            {
                response = await httpClient.SendAsync(request);

                // Log the response status code
                _logger.LogInformation($"Response Status Code: {response.StatusCode}");

                // Optionally, log the response body for successful requests
                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"Response Body: {responseBody}");
                }
            }
            catch (HttpRequestException e)
            {
                // Log more detailed error information here
                _logger.LogError($"{appVersion}::HTTPRequestException when calling {endpoint}: {e.Message}");
            }
            catch (Exception ex)
            {
                // Log unexpected errors
                _logger.LogError($"{appVersion}::Unexpected error when calling {endpoint}: {ex.Message}");
            }

            return response;
        }
    }
}
