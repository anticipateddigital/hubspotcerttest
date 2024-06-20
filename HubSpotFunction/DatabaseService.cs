using Microsoft.Extensions.Logging;
using System.Data.SqlClient;
using System.Reflection;

namespace MAPHubSpotFunction
{
    public class DatabaseService
    {
        private readonly string sqlConnectionString;
        private readonly ILogger<DatabaseService> _logger;
        private static readonly string appVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();

        public DatabaseService(ILogger<DatabaseService> logger, string sqlConnectionString)
        {
            this._logger = logger;
            this.sqlConnectionString = sqlConnectionString;
        }
        public async Task<(string vftStatus, int? cmsEmployeeCount, int? cmsRevenue, string companyEC, string companySC, string companyWL, string isActive, string activeBusiness, string isClient, string newClassCode, string comMajorClient, string cms_max_trndate, string cms_last_vft_invoice_date)?> FetchCompanyInfoFromDB(string hubspotRecordId)
        {
            using (var connection = new SqlConnection(sqlConnectionString))
            {
                await connection.OpenAsync();
                var query = "SELECT vft_status, Active, active_business, IsClient, cms_employee_count, cms_revenue_category, cms_com_assigned_ec, cms_com_assigned_sc, cms_com_assigned_wl, cms_new_class_code, com_major_client, cms_max_trndate, cms_last_vft_invoice_date FROM dbo.hubCLIENT WHERE hs_object_id = @HubspotRecordId";
                _logger.LogInformation($"{appVersion}::{query}");
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@HubspotRecordId", hubspotRecordId);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (reader.Read())
                        {
                            string vftStatus = reader["vft_status"] as string;
                            int? cmsEmployeeCount = Convert.ToInt32(reader["cms_employee_count"]);
                            int? cmsRevenue = Convert.ToInt32(reader["cms_revenue_category"]);
                            string companyEC = reader["cms_com_assigned_ec"] as string;
                            string companySC = reader["cms_com_assigned_sc"] as string;
                            string companyWL = reader["cms_com_assigned_wl"] as string;
                            string isActive = reader["Active"] as string;
                            string isActiveYN = isActive == "Yes" ? "Y" : (isActive == "No" ? "N" : null);
                            string activeBusiness = reader["active_business"] as string;
                            string isClient = reader["IsClient"] as string;
                            string newClassCode = reader["cms_new_class_code"] as string;
                            string comMajorClient = reader["com_major_client"] as string;
                            string cms_max_trndate_str = reader["cms_max_trndate"].ToString();
                            string cms_last_vft_invoice_date_str = reader["cms_last_vft_invoice_date"].ToString();
                            //string vftChurnStatus = reader["DEFINE"] as string;

                            // Log the variables
                            // _logger.LogInformation($"{appVersion}::VFT Status: {vftStatus}, CMS Employee Count: {cmsEmployeeCount}, Revenue Cat: {cmsRevenue}, EC: {companyEC}, SC: {companySC}");
                            
                            string cms_max_trndate = null; // Initialize as null for scope visibility
                            DateTime cms_max_trndate_fromdb;

                            if (!string.IsNullOrEmpty(cms_max_trndate_str) && DateTime.TryParse(cms_max_trndate_str, out cms_max_trndate_fromdb))
                            {
                                // Ensure the DateTime is treated as UTC
                                DateTime cms_max_trndate_utc = cms_max_trndate_fromdb.ToUniversalTime();

                                // Convert to Unix time milliseconds
                                long cms_max_trndate_unix = new DateTimeOffset(cms_max_trndate_utc).ToUnixTimeMilliseconds();
                                cms_max_trndate = cms_max_trndate_unix.ToString();
                            }
                            



                            string cms_last_vft_invoice_date = null; // Initialize as null for scope visibility
                            DateTime cms_last_vft_invoice_date_fromdb;

                            if (!string.IsNullOrEmpty(cms_last_vft_invoice_date_str) && DateTime.TryParse(cms_last_vft_invoice_date_str, out cms_last_vft_invoice_date_fromdb))
                            {
                                // Ensure the DateTime is treated as UTC
                                DateTime cms_last_vft_invoice_date_utc = cms_last_vft_invoice_date_fromdb.ToUniversalTime();

                                // Convert to Unix time milliseconds
                                long cms_last_vft_invoice_date_unix = new DateTimeOffset(cms_last_vft_invoice_date_utc).ToUnixTimeMilliseconds();
                                cms_last_vft_invoice_date = cms_last_vft_invoice_date_unix.ToString();
                            }
                            



                            // Return the tuple directly
                            return (vftStatus, cmsEmployeeCount, cmsRevenue, companyEC, companySC, companyWL, isActiveYN, activeBusiness, isClient, newClassCode, comMajorClient, cms_max_trndate, cms_last_vft_invoice_date);
                        }

                    }
                }
            }
            return null;
        }
        public async Task<(string email, string phone, string firstName, string lastName, int? titleCode, string company_code, int? client_number, string workshop_date, string cms_api_vftmember, string cms_api_vftactive)?> FetchContactInfoFromDB(string hubspotRecordId)
        {
            using (var connection = new SqlConnection(sqlConnectionString))
            {
                await connection.OpenAsync();
                var query = "SELECT email, phone, title_code, firstname, lastname, company_code, client_number, workshop_date, cms_api_vftmember, cms_api_vftactive FROM dbo.hubCUSTOMER WHERE hs_object_id = @HubspotRecordId";
                _logger.LogInformation($"{appVersion}::{query}");
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@HubspotRecordId", hubspotRecordId);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (reader.Read())
                        {
                            string email = reader["email"] as string;
                            string phone = reader["phone"] as string;
                            string firstName = reader["firstname"] as string;
                            string lastName = reader["lastname"] as string;
                            int? titleCode = Convert.ToInt32(reader["title_code"]);
                            string company_code = reader["company_code"] as string;
                            int? client_number = Convert.ToInt32(reader["client_number"]);
                            string cms_api_vftmember = reader["cms_api_vftmember"] as string;
                            string cms_api_vftactive = reader["cms_api_vftactive"] as string;
                            string workshop_date_str = reader["workshop_date"].ToString();

                            // Log extracted variables individually 
                            _logger.LogInformation("Email: {0}", email);
                            _logger.LogInformation("Phone: {0}", phone);
                            _logger.LogInformation("First Name: {0}", firstName);
                            _logger.LogInformation("Last Name: {0}", lastName);
                            _logger.LogInformation("Title Code: {0}", titleCode);
                            _logger.LogInformation("Company Code: {0}", company_code);
                            _logger.LogInformation("Client Number: {0}", client_number);
                            _logger.LogInformation("Workshop Date: {0}", workshop_date_str);
                            _logger.LogInformation("CMS API VFT Member: {0}", cms_api_vftmember);
                            _logger.LogInformation("CMS API VFT Active: {0}", cms_api_vftactive);

                            
                            string workshop_date_unix_str = null; // Initialize as null for scope visibility
                            DateTime workshop_date_fromdb;

                            if (!string.IsNullOrEmpty(workshop_date_str) && DateTime.TryParse(workshop_date_str, out workshop_date_fromdb))
                            {
                                // Ensure the DateTime is treated as UTC
                                DateTime workshop_date_utc = workshop_date_fromdb.ToUniversalTime();

                                // Convert to Unix time milliseconds
                                long workshop_date_unix = new DateTimeOffset(workshop_date_utc).ToUnixTimeMilliseconds();
                                workshop_date_unix_str = workshop_date_unix.ToString();
                            }
                            



                            // Return the tuple directly, using the converted workshop_date_unix_str for the workshop_date field
                            return (email, phone, firstName, lastName, titleCode, company_code, client_number, workshop_date_unix_str, cms_api_vftmember, cms_api_vftactive);
                        }
                    }
                }
            }
            return null;
        }
        public async Task UpdateSqlServer(string referenceNumber, string hubspotRecordId, string entityType)
        {
            string tableName = entityType == "contacts" ? "customer" : "company";
            string referenceColumn = entityType == "contacts" ? "cst_ref_no" : "com_map_id";
            string hubIdColumn = entityType == "contacts" ? "cst_hub_id" : "com_hub_id";

            using (var connection = new SqlConnection(sqlConnectionString))
            {
                await connection.OpenAsync();
                // Check if the current hs_object_id matches the new one to decide if an update is needed
                var currentHubspotIdQuery = $"SELECT {hubIdColumn} FROM {tableName} WHERE {referenceColumn} = @ReferenceNumber";
                _logger.LogInformation($"{appVersion}:: {currentHubspotIdQuery}");
                string currentHubspotId = null;
                using (var command = new SqlCommand(currentHubspotIdQuery, connection))
                {
                    command.Parameters.AddWithValue("@ReferenceNumber", referenceNumber);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (reader.Read())
                        {
                            currentHubspotId = reader[hubIdColumn] as string;
                        }
                    }
                }

                if (currentHubspotId == hubspotRecordId)
                {
                    _logger.LogInformation($"{appVersion}::No update needed for {referenceNumber}.");
                    return;
                }

                // If the current and new hs_object_ids do not match, update the database
                var updateCommand = $"UPDATE {tableName} SET {hubIdColumn} = @HubspotRecordId WHERE {referenceColumn} = @ReferenceNumber";
                using (var updateCmd = new SqlCommand(updateCommand, connection))
                {
                    updateCmd.Parameters.AddWithValue("@ReferenceNumber", referenceNumber);
                    updateCmd.Parameters.AddWithValue("@HubspotRecordId", hubspotRecordId);
                    int rowsAffected = await updateCmd.ExecuteNonQueryAsync();
                    _logger.LogInformation($"{appVersion}:: Rows Affected: {rowsAffected}");
                    if (rowsAffected > 0)
                    {
                        _logger.LogInformation($"{appVersion}::SQL Update successful for {entityType}: {referenceNumber}.");
                    }
                    else
                    {
                        _logger.LogWarning($"{appVersion}::No rows updated for {entityType}: {referenceNumber}. Record might not exist or already up to date.");
                    }
                }
            }
        }
    }
}
