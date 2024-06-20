using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Reflection;

namespace MAPHubSpotFunction
{
    public class HubSpotApiService
    {
        private readonly ILogger<HubSpotApiService> _logger;
        private static readonly string appVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        private static readonly string accessToken = Environment.GetEnvironmentVariable("HubSpotAccessToken");
        private readonly HttpHandler _httpHandler;
        private readonly Utility _utility;
        private readonly DatabaseService _databaseService;

        public HubSpotApiService(ILoggerFactory loggerFactory, IHttpClientFactory httpClientFactory, string sqlConnectionString)
        {
            var httpClient = httpClientFactory.CreateClient();
            httpClient.BaseAddress = new Uri("https://api.hubapi.com");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            _logger = loggerFactory.CreateLogger<HubSpotApiService>();
            _httpHandler = new HttpHandler(loggerFactory.CreateLogger<HttpHandler>(), httpClient);
            _utility = new Utility();
            _databaseService = new DatabaseService(loggerFactory.CreateLogger<DatabaseService>(), sqlConnectionString);
        }

        public async Task GetRecordsFromHubSpot(string entityType)
        {
            int currentOffset = 0;
            bool hasMore = true;

            while (hasMore)
            {
                string searchEndpoint = entityType == "contacts" ? "/crm/v3/objects/contacts/search" : "/crm/v3/objects/companies/search";
                JObject searchPayload = _utility.BuildSearchPayload(entityType, currentOffset);
                JObject responseContent = await _httpHandler.PostRequestAsync(searchEndpoint, searchPayload);
                JArray entities = (JArray)responseContent["results"];

                if (entities.Count > 99)
                {
                    foreach (JObject entity in entities)
                    {
                        await ProcessEntityAsync(entity, entityType);
                    }
                    currentOffset += 100;
                }
                else
                {
                    hasMore = false;
                }
            }
            _logger.LogInformation($"{appVersion}::{entityType} processed successfully.");
        }
        public async Task ProcessEntityAsync(JObject entity, string entityType)
        {
            _logger.LogInformation($"{appVersion}::Processing {entityType}: {entity}");
            string referenceNumber = entity["cst_ref_no"]?.ToString() ?? entity["properties"]?["cst_ref_no"]?.ToString() ?? entity["properties"]?["cms_client_number"]?.ToString() ?? entity["cms_client_number"]?.ToString();
            string hubspotRecordId = entity["id"]?.ToString() ?? entity["objectId"]?.ToString();

            await _databaseService.UpdateSqlServer(referenceNumber, hubspotRecordId, entityType);

            if (!string.IsNullOrEmpty(referenceNumber) && !string.IsNullOrEmpty(hubspotRecordId))
            {
                _logger.LogInformation($"{appVersion}::Processing {entityType} entity: {hubspotRecordId}.");

                if (entityType == "companies")
                {
                    var companyInfo = await _databaseService.FetchCompanyInfoFromDB(hubspotRecordId);
                    _logger.LogInformation($"{appVersion}::CP156 {companyInfo} Type {entityType}");
                    if (companyInfo != null)
                    {
                        await UpdateHubSpotCompanyProperties(hubspotRecordId, DateTime.UtcNow, companyInfo);
                        _logger.LogInformation($"{appVersion}::HubSpot Company Record ID: {hubspotRecordId} Synced.");
                    }
                }
                else if (entityType == "contacts")
                {
                    var contactInfo = await _databaseService.FetchContactInfoFromDB(hubspotRecordId);
                    _logger.LogInformation($"{appVersion}::CP166 {contactInfo} Type {entityType}");
                    if (contactInfo != null)
                    {
                        await UpdateHubSpotContactProperties(hubspotRecordId, DateTime.UtcNow, contactInfo);
                        _logger.LogInformation($"HubSpot Contact Record ID: {hubspotRecordId} Synced.");
                    }
                }
                else
                {
                    await UpdateHubSpotDateProperty(hubspotRecordId, DateTime.UtcNow, entityType);
                }
            }
            else
            {
                _logger.LogWarning($"{appVersion}::Skipped {entityType} entity due to missing data.");
            }
        }
        private async Task UpdateHubSpotCompanyProperties(string hubspotRecordId, DateTime date, (string vftStatus, int? cmsEmployeeCount, int? cmsRevenue, string companyEC, string companySC, string companyWL, string isActiveYN, string activeBusiness, string isClient, string newClassCode, string comMajorClient, string cms_max_trndate, string cms_last_vft_invoice_date)? companyInfo)
        {
            if (companyInfo == null) return; // Exit if no company info was found

            DateTime dateAtMidnightUtc = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Utc);
            long unixTimeMilliseconds = ((DateTimeOffset)dateAtMidnightUtc).ToUnixTimeMilliseconds();

            var payload = new JObject
            {
                ["properties"] = new JObject
                {
                    ["cms_last_synced"] = unixTimeMilliseconds,
                    ["vft_status"] = companyInfo.Value.vftStatus,
                    ["cms_employee_count"] = companyInfo.Value.cmsEmployeeCount,
                    ["cms_revenue_category"] = companyInfo.Value.cmsRevenue,
                    ["cms_com_assigned_ec"] = companyInfo.Value.companyEC,
                    ["cms_com_assigned_sc"] = companyInfo.Value.companySC,
                    ["cms_com_assigned_wl"] = companyInfo.Value.companyWL,
                    ["company_activestatus"] = companyInfo.Value.isActiveYN,
                    ["active_business"] = companyInfo.Value.activeBusiness,
                    ["isclient"] = companyInfo.Value.isClient,
                    ["cms_new_class_code"] = companyInfo.Value.newClassCode,
                    ["com_major_client"] = companyInfo.Value.comMajorClient,
                    ["cms_max_trndate"] = companyInfo.Value.cms_max_trndate,
                    ["cms_last_vft_invoice_date"] = companyInfo.Value.cms_last_vft_invoice_date
                }
            };

            string endpoint = $"/crm/v3/objects/companies/{hubspotRecordId}";
            _logger.LogInformation($"{appVersion}::Updating HubSpot company properties for ID: {hubspotRecordId}.");
            var response = await _httpHandler.PatchRequestAsync(endpoint, payload);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation($"{appVersion}::HubSpot company {hubspotRecordId} properties updated successfully.");
            }
            else
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogError($"{appVersion}::Failed to update HubSpot company {hubspotRecordId} properties. Response: {responseBody}");
            }
        }
        private async Task UpdateHubSpotContactProperties(string hubspotRecordId, DateTime date, (string email, string phone, string firstName, string lastName, int? titleCode, string company_code, int? client_number, string workshop_date, string cms_api_vftmember, string cms_api_vftactive)? contactInfo)
        {
            if (contactInfo == null) return; // Exit if no contact info was found

            DateTime dateAtMidnightUtc = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Utc);
            long unixTimeMilliseconds = ((DateTimeOffset)dateAtMidnightUtc).ToUnixTimeMilliseconds();

            var payload = new JObject
            {
                ["properties"] = new JObject
                {
                    ["cms_last_synced"] = unixTimeMilliseconds,
                    ["email"] = contactInfo.Value.email,
                    ["firstname"] = contactInfo.Value.firstName,
                    ["lastname"] = contactInfo.Value.lastName,
                    ["phone"] = contactInfo.Value.phone,
                    ["title_code"] = contactInfo.Value.titleCode,
                    ["company_code"] = contactInfo.Value.company_code,
                    ["client_number"] = contactInfo.Value.client_number,
                    ["workshop_date"] = contactInfo.Value.workshop_date,
                    ["cms_api_vftmember"] = contactInfo.Value.cms_api_vftmember,  
                    ["cms_api_vftactive"] = contactInfo.Value.cms_api_vftactive
                }
            };


            _logger.LogInformation($"Payload: {payload}");

            string endpoint = $"/crm/v3/objects/contacts/{hubspotRecordId}";
            _logger.LogInformation($"Updating HubSpot contact properties for ID: {hubspotRecordId}.");
            var response = await _httpHandler.PatchRequestAsync(endpoint, payload);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation($"{appVersion}::HubSpot contact {hubspotRecordId} properties updated successfully.");
            }
            else
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogError($"{appVersion}::Failed to update HubSpot contact {hubspotRecordId} properties. Response: {responseBody}");
            }
        }
        private async Task UpdateHubSpotDateProperty(string hubspotRecordId, DateTime date, string entityType)
        {
            DateTime dateAtMidnightUtc = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Utc);
            long unixTimeMilliseconds = ((DateTimeOffset)dateAtMidnightUtc).ToUnixTimeMilliseconds();

            string endpoint = entityType == "contacts" ?
                $"/crm/v3/objects/contacts/{hubspotRecordId}" :
                $"/crm/v3/objects/companies/{hubspotRecordId}";

            var payload = new JObject
            {
                ["properties"] = new JObject
                {
                    ["cms_last_synced"] = unixTimeMilliseconds
                }
            };

            _logger.LogInformation($"Updating HubSpot {entityType} last synced field for ID: {hubspotRecordId}.");
            var response = await _httpHandler.PatchRequestAsync(endpoint, payload);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation($"{appVersion}::HubSpot {entityType} {hubspotRecordId} last synced field updated successfully.");
            }
            else
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogError($"{appVersion}::Failed to update HubSpot {entityType} {hubspotRecordId} last synced field. Response: {responseBody}");
            }
        }
        public async Task SyncCompanyInfoWithHubSpot(string hubspotRecordId)
        {
            var companyInfo = await _databaseService.FetchCompanyInfoFromDB(hubspotRecordId);
            _logger.LogWarning($"{appVersion}::CP386 .");

            if (companyInfo.HasValue)
            {
                await UpdateHubSpotCompanyProperties(hubspotRecordId, DateTime.UtcNow, companyInfo);
                _logger.LogWarning($"{appVersion}::Update to Company Properties Complete.");
            }
            else
            {
                _logger.LogWarning($"{appVersion}::No company info found to update HubSpot.");
            }
        }
        public async Task SyncContactInfoWithHubSpot(string hubspotRecordId)
        {
            var contactInfo = await _databaseService.FetchContactInfoFromDB(hubspotRecordId);

            if (contactInfo.HasValue)
            {
                await UpdateHubSpotContactProperties(hubspotRecordId, DateTime.UtcNow, contactInfo);
            }
            else
            {
                _logger.LogWarning("No contact info found to update HubSpot.");
            }
        }
    }
}
