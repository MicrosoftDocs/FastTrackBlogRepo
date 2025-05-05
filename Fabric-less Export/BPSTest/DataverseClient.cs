namespace BPSTest
{
    using System.Net.Http.Headers;
    using System.Text.Json;
    using System.Web;
    using Microsoft.Identity.Client;

    internal class DataverseClient
    {
        public DataverseClient(string orgApiUrl)
        {
            OrgApiUrl = orgApiUrl;
        }

        public string OrgApiUrl { get; set; }

        public async Task<Uri> GetMdlAccessUri(bool useBps = false)
        {
            const int tokenValidityInMinutes = 60; // Maximum value allowed by MDL is 60 minutes.

            // Most of the code copied from: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/quick-start-console-app-csharp
            // Microsoft Entra ID app registration shared by all Power App samples.
            var clientId = "51f81489-12ee-4a9e-aaae-a2591f45987d";
            var redirectUri = "http://localhost"; // Loopback for the interactive login.

            var authBuilder =
                PublicClientApplicationBuilder
                    .Create(clientId)
                    .WithAuthority(AadAuthorityAudience.AzureAdMultipleOrgs)
                    .WithRedirectUri(redirectUri)
                    .Build();

            var scope = this.OrgApiUrl + "/user_impersonation";
            string[] scopes = { scope };

            AuthenticationResult token =
               await authBuilder.AcquireTokenInteractive(scopes).ExecuteAsync();

            var client = new HttpClient
            {
                // See https://docs.microsoft.com/powerapps/developer/data-platform/webapi/compose-http-requests-handle-errors#web-api-url-and-versions
                BaseAddress = new Uri(this.OrgApiUrl + "/api/data/v9.2/"),
                Timeout = new TimeSpan(0, 2, 0)    // Standard two minute timeout on web service calls.
            };

            // Default headers for each Web API call.
            // See https://docs.microsoft.com/powerapps/developer/data-platform/webapi/compose-http-requests-handle-errors#http-headers
            HttpRequestHeaders headers = client.DefaultRequestHeaders;
            headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
            headers.Add("OData-MaxVersion", "4.0");
            headers.Add("OData-Version", "4.0");
            headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.GetAsync("datalakefolders?$filter=name%20eq%20%27Customer%20Insights%20Journeys%27&$select=path,name");
            response.EnsureSuccessStatusCode();
            string dataLakeFolderPath;
            using (JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync()))
            {
                JsonElement root = doc.RootElement;
                dataLakeFolderPath = root.GetProperty("value").EnumerateArray().First().GetProperty("path").GetString()!;
                Console.WriteLine($"Found data lake folder path: {dataLakeFolderPath}");
            }

            response = await client.GetAsync("RetrieveAnalyticsStoreDetails");
            response.EnsureSuccessStatusCode();
            string containerBaseUrl;
            using (JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync()))
            {
                JsonElement root = doc.RootElement;
                containerBaseUrl = root.GetProperty("AnalyticsStoreDetails").GetProperty("Endpoint").GetString()!;
                Console.WriteLine($"Found base container URL: {containerBaseUrl}");
            }

            var containerUrl = $"{containerBaseUrl}/{dataLakeFolderPath}";
            response = await client.GetAsync($"RetrieveAnalyticsStoreAccess(Url=@p1,ResourceType='Folder',Permissions='Read,List',SasTokenValidityInMinutes={tokenValidityInMinutes},UseBlobProxy={useBps.ToString().ToLowerInvariant()})?@p1={HttpUtility.UrlEncode("'" + containerUrl + "'")}");
            response.EnsureSuccessStatusCode();
            using (JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync()))
            {
                JsonElement root = doc.RootElement;
                var mdlToken = root.GetProperty("SASToken").GetString()!;

                if (useBps)
                {
                    return new Uri(mdlToken);
                }
                else
                {
                    return new Uri(new Uri(containerUrl), mdlToken);
                }
            }
        }
    }
}
