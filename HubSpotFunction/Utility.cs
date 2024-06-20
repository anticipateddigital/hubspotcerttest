using Newtonsoft.Json.Linq;

namespace MAPHubSpotFunction
{
    public class Utility
    {
        public JObject BuildSearchPayload(string entityType, int currentOffset)
        {
            string propertyName = entityType == "contacts" ? "cst_ref_no" : "cms_client_number";
            return new JObject
            {
                ["filterGroups"] = new JArray(new JObject
                {
                    ["filters"] = new JArray(new JObject
                    {
                        ["propertyName"] = propertyName,
                        ["operator"] = "GT",
                        ["value"] = "0"
                    })
                }),
                ["properties"] = new JArray(propertyName),
                ["limit"] = 100,
                ["after"] = currentOffset
            };
        }
    }
}
