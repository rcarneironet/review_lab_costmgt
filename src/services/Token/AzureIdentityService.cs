using Azure.Core;
using Azure.Identity;
using System.Threading.Tasks;

namespace services.Token
{
    public class AzureIdentityTokenRequestModel
    {
        public string TenantId { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
    }

    public static class AzureIdentityService
    {
        public static async Task<string> GetToken(AzureIdentityTokenRequestModel model)
        {
            var credentials = new ClientSecretCredential(model.TenantId, model.ClientId, model.ClientSecret);
            var token = await credentials.GetTokenAsync(
                new TokenRequestContext(new[] { "https://management.azure.com/.default" }, tenantId: model.TenantId)
                );
            return token.Token;
        }
    }
}
