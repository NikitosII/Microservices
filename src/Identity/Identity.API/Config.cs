using Duende.IdentityServer;
using Duende.IdentityServer.Models;

namespace Identity.API
{
    public static class Config
    {
        public static IEnumerable<IdentityResource> IdentityResources =>
            new IdentityResource[]
            {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
                new IdentityResource("roles", new[] { "role" })
            };

        public static IEnumerable<ApiScope> ApiScopes =>
            new ApiScope[]
            {
                new ApiScope("gateway", "Gateway API"),
                new ApiScope("product.api", "Product API"),
                new ApiScope("order.api", "Order API"),
                new ApiScope("cart.api", "Shopping Cart API"),
                new ApiScope("email.api", "Email API"),
                new ApiScope("payment.api", "Payment API"),
                new ApiScope("coupon.api", "Coupon API")
            };

        public static IEnumerable<Client> Clients =>
            new Client[]
            {
                // React client
                new Client
                {
                    ClientId = "react.client",
                    ClientName = "React Client",
                    AllowedGrantTypes = GrantTypes.Code,
                    RequireClientSecret = false,
                    RedirectUris = { "http://localhost:3000/callback" },
                    PostLogoutRedirectUris = { "http://localhost:3000" },
                    AllowedCorsOrigins = { "http://localhost:3000" },
                    AllowedScopes =
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        "gateway",
                        "product.api",
                        "order.api",
                        "cart.api"
                    },
                    RequirePkce = true,
                    AllowAccessTokensViaBrowser = true
                },
                
                // Gateway client
                new Client
                {
                    ClientId = "gateway.client",
                    ClientName = "Gateway Client",
                    ClientSecrets = { new Secret("secret".Sha256()) },
                    AllowedGrantTypes = GrantTypes.ClientCredentials,
                    AllowedScopes =
                    {
                        "product.api",
                        "order.api",
                        "cart.api",
                        "email.api",
                        "payment.api",
                        "coupon.api"
                    }
                }
            };
    }
}