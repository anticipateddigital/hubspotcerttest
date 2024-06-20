using MAPHubSpotFunction;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace HubSpotFunction
{
    public class HubSpotSynctoDBFunction
    {
        private static readonly string appVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();

        private static readonly string sqlConnectionString = Environment.GetEnvironmentVariable("SqlConnectionString");
        private readonly ILogger _logger;
        private readonly HubSpotApiService _hubSpotApiService;

        public HubSpotSynctoDBFunction(ILoggerFactory loggerFactory, IHttpClientFactory httpClientFactory)
        {
            _logger = loggerFactory.CreateLogger<HubSpotSynctoDBFunction>();
            _hubSpotApiService = new HubSpotApiService(loggerFactory, httpClientFactory, sqlConnectionString);
        }

        [Function("BatchSyncHubSpot")]
        public async Task<IActionResult> ProcessPayloadAsync([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            _logger.LogInformation($"{appVersion}::Processing started.");
            try
            {
                await _hubSpotApiService.GetRecordsFromHubSpot("companies");
                await _hubSpotApiService.GetRecordsFromHubSpot("contacts");
                return new OkObjectResult("Processing completed successfully.");
            }
            catch (Exception e)
            {
                _logger.LogError($"{appVersion}::Error during processing: {e.Message}");
                return new BadRequestObjectResult($"Error during processing: {e.Message}");
            }
        }

        [Function("HubSpotSyncToDB")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
        {
            _logger.LogInformation($"{appVersion}::Incoming HubSpot API Webhook");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            try
            {
                var token = JToken.Parse(requestBody);

                if (token.Type == JTokenType.Array)
                {
                    // Handle JSON array
                    var payloads = JArray.Parse(requestBody);
                    foreach (var payload in payloads)
                    {
                        await ProcessSinglePayloadAsync(payload as JObject);
                    }
                    return new OkObjectResult($"{payloads.Count} payloads processed successfully.");
                }
                else if (token.Type == JTokenType.Object)
                {
                    // Handle JSON object
                    var payload = JObject.Parse(requestBody);
                    await ProcessSinglePayloadAsync(payload);
                    return new OkObjectResult("Single payload processed successfully.");
                }
                else
                {
                    _logger.LogError($"{appVersion}::Payload is neither an object nor an array.");
                    return new BadRequestObjectResult("Payload format not supported.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"{appVersion}::Error processing payload: {ex.Message}");
                return new BadRequestObjectResult($"Error processing payload: {ex.Message}");
            }
        }
        private async Task ProcessSinglePayloadAsync(JObject payload)
        {
            _logger.LogInformation($"Processing payload: {payload}");

            string entityType = null;
            string id = payload["id"]?.ToString();
            string hs_object_id = payload["hs_object_id"]?.ToString();

            // Determine which property we're dealing with and assign the value accordingly
            string cst_ref_no = payload["cst_ref_no"]?.ToString();
            string cms_client_number = payload["cms_client_number"]?.ToString();

            if (!string.IsNullOrWhiteSpace(cst_ref_no))
            {
                entityType = "contacts";
            }
            else if (!string.IsNullOrWhiteSpace(cms_client_number))
            {
                entityType = "companies";
            }

            _logger.LogInformation($"{appVersion}::Entity Type:{entityType}");


            // Log the received payload 
            _logger.LogInformation($"{appVersion}::Received payload: id={id}, hs_object_id={hs_object_id}, cst_ref_no={cst_ref_no}, cms_client_number={cms_client_number}, entityType={entityType}");

            JObject logObject = new JObject
            {
                ["id"] = id,
                ["hs_object_id"] = hs_object_id,
                ["entityType"] = entityType
            };
            if (!string.IsNullOrEmpty(cst_ref_no))
            {
                logObject["cst_ref_no"] = cst_ref_no;
            }
            if (!string.IsNullOrEmpty(cms_client_number))
            {
                logObject["cms_client_number"] = cms_client_number;
            }

            await _hubSpotApiService.ProcessEntityAsync(logObject, entityType);

            _logger.LogInformation($"{appVersion}::Payload for hs_object_id={hs_object_id} payload processed successfully.");
        }
    }
}

