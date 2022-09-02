using services.Dtos;
using services.Email;
using services.Token;
using services.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace services.APIs.CostManagement
{
    public static class CostManagementService
    {
        public static async Task AzureBillingMonthToDateApiFetch()
        {

            if (string.IsNullOrEmpty(Utils.subscriptionIdList))
                throw new Exception("Subscription list is empty");

            var subscriptionList = Utils.subscriptionIdList.Split(',').ToList<string>();

            using var client = new HttpClient();
            var jsonContent = new StringContent(
                GetBillingMonthToDateJson(),
                Encoding.UTF8,
                "application/json");

            var token = await AzureIdentityService.GetToken(new AzureIdentityTokenRequestModel
            {
                TenantId = Utils.TenantId,
                ClientId = Utils.ClientId,
                ClientSecret = Utils.ClientSecret
            });

            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

            try
            {
                foreach (var sub in subscriptionList)
                {
                    var url = "https://management.azure.com/subscriptions/" + sub + "/providers/Microsoft.CostManagement/query?api-version=2021-10-01";
                    var result = await client.PostAsync(url.TrimStart().TrimEnd(), jsonContent);
                    var jsonData = await result.Content.ReadFromJsonAsync<JsonElement>();

                    if (jsonData.GetProperty("properties").GetProperty("rows").ToString() != "[]")
                    {
                        var rows = jsonData.GetProperty("properties").GetProperty("rows").EnumerateArray();

                        var billing = new BillingDto
                        {
                            Date = DateTime.Now.Date,
                            SubscriptionId = sub,
                            Value = Math.Round(rows.First().EnumerateArray().ElementAt(0).GetDouble(), 2),
                            IsUpdate = false
                        };

                        var service = new CostManagementDataService();

                        //Check if there is already a cost for the day, if there is, update it, otherwise insert a new value
                        var todaysValue = await service.GetLatestBillingForToday(sub);
                        if (todaysValue.SubscriptionId != null)
                        {
                            billing.Value = billing.Value;
                            billing.IsUpdate = true;
                            //find percentual of increase between last value and current value
                            billing.PercentChanged = Math.Round(((todaysValue.Value - billing.Value) / billing.Value) * 100, 2);
                        }
                        //Save to SQL Database
                        await service.SaveBilling(billing);
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        internal static string GetBillingMonthToDateJson()
        {
            return "{ \"type\": \"Usage\", \"timeframe\": \"BillingMonthToDate\", \"dataset\": { \"granularity\": \"None\", \"aggregation\": { \"totalCost\": { \"name\": \"PreTaxCost\", \"function\": \"Sum\"}  } } }";
        }

        public static async Task<List<WeeklyBillingDto>> GetWeeklyBilling()
        {
            var service = new CostManagementDataService();
            return await service.GetWeeklyBilling();
        }

        public static async Task<List<MonthToDateDto>> GetMonthToDateBilling()
        {
            var service = new CostManagementDataService();
            return await service.GetMonthToDateBilling();
        }

        public static void NotifyConsumptionIncreaseByEmail()
        {
            var service = new CostManagementDataService();
            var notifications = service.NotifyConsumptionIncreaseByEmail();

            foreach (var item in notifications)
            {
                if (!item.IsEmailSent && item.ValueChangePercent >= Utils.PercentageWarning)
                {
                    new EmailService().SendEmail(item.SubscriptionId, item.ValueChangePercent.ToString());
                    new CostManagementDataService().UpdateEmailNotification(item.Id);
                }
            }

        }

    }
}
