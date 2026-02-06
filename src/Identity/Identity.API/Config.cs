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
                new IdentityResources.Email(),
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

        public static IEnumerable<ApiResource> ApiResources =>
            new ApiResource[]
            {
                new ApiResource("gateway", "Gateway API")
                {
                    Scopes = { "gateway" },
                    UserClaims = { "role", "name", "email" }
                },
                new ApiResource("product.api", "Product API")
                {
                    Scopes = { "product.api" },
                    UserClaims = { "role", "name", "email" }
                },
                new ApiResource("order.api", "Order API")
                {
                    Scopes = { "order.api" },
                    UserClaims = { "role", "name", "email" }
                },
                new ApiResource("cart.api", "Shopping Cart API")
                {
                    Scopes = { "cart.api" },
                    UserClaims = { "role", "name", "email" }
                },
                new ApiResource("email.api", "Email API")
                {
                    Scopes = { "email.api" },
                    UserClaims = { "role", "name", "email" }
                },
                new ApiResource("payment.api", "Payment API")
                {
                    Scopes = { "payment.api" },
                    UserClaims = { "role", "name", "email" }
                },
                new ApiResource("coupon.api", "Coupon API")
                {
                    Scopes = { "coupon.api" },
                    UserClaims = { "role", "name", "email" }
                }
            };

        public static IEnumerable<Client> Clients =>
            new Client[]
            {
                // React client (SPA)
                new Client
                {
                    ClientId = "react.client",
                    ClientName = "React Client",
                    AllowedGrantTypes = GrantTypes.Code,
                    RequireClientSecret = false,

                    RedirectUris =
                    {
                        "http://localhost:3000/callback",
                        "http://localhost:3000/silent-renew.html"
                    },
                    PostLogoutRedirectUris = { "http://localhost:3000" },
                    AllowedCorsOrigins = { "http://localhost:3000" },

                    AllowedScopes =
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.Email,
                        "roles",
                        "gateway",
                        "product.api",
                        "order.api",
                        "cart.api"
                    },

                    RequirePkce = true,
                    AllowAccessTokensViaBrowser = true,
                    AccessTokenLifetime = 3600, // 1 hour
                    IdentityTokenLifetime = 3600,
                    RefreshTokenUsage = TokenUsage.ReUse,
                    RefreshTokenExpiration = TokenExpiration.Sliding,
                    SlidingRefreshTokenLifetime = 1296000, // 15 days
                    
                    // Mark as SPA client
                    Properties = new Dictionary<string, string>
                    {
                        { "NativeClient", "false" }
                    }
                },
                
                // Gateway client (machine-to-machine)
                new Client
                {
                    ClientId = "gateway.client",
                    ClientName = "Gateway Client",
                    ClientSecrets = { new Secret("gateway_secret".Sha256()) },
                    AllowedGrantTypes = GrantTypes.ClientCredentials,

                    AllowedScopes =
                    {
                        "product.api",
                        "order.api",
                        "cart.api",
                        "email.api",
                        "payment.api",
                        "coupon.api"
                    },

                    AccessTokenLifetime = 3600
                },
                
                // Service-to-service clients
                new Client
                {
                    ClientId = "order.service",
                    ClientName = "Order Service",
                    ClientSecrets = { new Secret("order_secret".Sha256()) },
                    AllowedGrantTypes = GrantTypes.ClientCredentials,

                    AllowedScopes =
                    {
                        "product.api",
                        "coupon.api",
                        "email.api",
                        "payment.api"
                    }
                },

                new Client
                {
                    ClientId = "product.service",
                    ClientName = "Product Service",
                    ClientSecrets = { new Secret("product_secret".Sha256()) },
                    AllowedGrantTypes = GrantTypes.ClientCredentials,

                    AllowedScopes =
                    {
                        "gateway"
                    }
                },

                // Swagger/Testing client (Resource Owner Password)
                new Client
                {
                    ClientId = "swagger-client",
                    ClientName = "Swagger Testing Client",
                    AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
                    RequireClientSecret = false,

                    AllowedScopes =
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.Email,
                        "roles",
                        "gateway",
                        "product.api",
                        "order.api",
                        "cart.api",
                        "coupon.api",
                        "payment.api"
                    },

                    AccessTokenLifetime = 3600, // 1 hour
                    AllowOfflineAccess = true
                }
            };
    }
}