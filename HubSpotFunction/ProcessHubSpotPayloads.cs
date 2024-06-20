/*
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace HubSpotFunction
{
    public class PayloadProcessor
    {
        private static readonly string appVersion = "1.2.604"; //v1.3.1 STABLE - Bidirectional Payloads
        private readonly ILogger _logger;
        
        public PayloadProcessor(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<PayloadProcessor>();
            
        }

        [Function("ProcessHubSpotPayloads")]
        public async Task ProcessHubSpotPayloads(
            [QueueTrigger("hubspot-queue", Connection = "AzureWebJobsStorage")] string queueItem,
            FunctionContext context)
        {
            _logger.LogInformation($"Processing queued payload.");

            JObject payload = JObject.Parse(queueItem);

            // Your payload processing logic here

            _logger.LogInformation($"Payload processed: {payload}");
        }
    }
}
*/
