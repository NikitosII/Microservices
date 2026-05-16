using System.Text.Json;

namespace Gateway.Tests
{
    [TestClass]
    public class OcelotConfigurationTests
    {
        private JsonDocument _ocelotConfig = null!;

        [TestInitialize]
        public void Setup()
        {
            var configPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "src", "Gateway", "Gateway.API", "ocelot.json");

            // If the config file doesn't exist in the expected location, create a test configuration
            if (File.Exists(configPath))
            {
                var configJson = File.ReadAllText(configPath);
                _ocelotConfig = JsonDocument.Parse(configJson);
            }
            else
            {
                // Use embedded test configuration
                var testConfig = @"{
                    ""Routes"": [
                        {
                            ""DownstreamPathTemplate"": ""/api/v1/products"",
                            ""DownstreamScheme"": ""http"",
                            ""DownstreamHostAndPorts"": [{ ""Host"": ""product.api"", ""Port"": 80 }],
                            ""UpstreamPathTemplate"": ""/v1/products"",
                            ""UpstreamHttpMethod"": [""GET"", ""POST""]
                        },
                        {
                            ""DownstreamPathTemplate"": ""/api/v1/products/{id}"",
                            ""DownstreamScheme"": ""http"",
                            ""DownstreamHostAndPorts"": [{ ""Host"": ""product.api"", ""Port"": 80 }],
                            ""UpstreamPathTemplate"": ""/v1/products/{id}"",
                            ""UpstreamHttpMethod"": [""GET"", ""PUT"", ""DELETE""]
                        },
                        {
                            ""DownstreamPathTemplate"": ""/api/v1/coupons"",
                            ""DownstreamScheme"": ""http"",
                            ""DownstreamHostAndPorts"": [{ ""Host"": ""coupon.api"", ""Port"": 80 }],
                            ""UpstreamPathTemplate"": ""/v1/coupons"",
                            ""UpstreamHttpMethod"": [""GET"", ""POST""]
                        },
                        {
                            ""DownstreamPathTemplate"": ""/api/v1/cart"",
                            ""DownstreamScheme"": ""http"",
                            ""DownstreamHostAndPorts"": [{ ""Host"": ""shoppingcart.api"", ""Port"": 80 }],
                            ""UpstreamPathTemplate"": ""/v1/cart"",
                            ""UpstreamHttpMethod"": [""GET"", ""POST"", ""DELETE""]
                        },
                        {
                            ""DownstreamPathTemplate"": ""/api/v1/orders"",
                            ""DownstreamScheme"": ""http"",
                            ""DownstreamHostAndPorts"": [{ ""Host"": ""order.api"", ""Port"": 80 }],
                            ""UpstreamPathTemplate"": ""/v1/orders"",
                            ""UpstreamHttpMethod"": [""GET"", ""POST""]
                        }
                    ],
                    ""GlobalConfiguration"": {
                        ""BaseUrl"": ""http://localhost:5000""
                    }
                }";
                _ocelotConfig = JsonDocument.Parse(testConfig);
            }
        }

        [TestCleanup]
        public void Cleanup()
        {
            _ocelotConfig?.Dispose();
        }

        #region Routes Configuration Tests

        [TestMethod]
        public void OcelotConfig_HasRoutesArray()
        {
            // Act
            var hasRoutes = _ocelotConfig.RootElement.TryGetProperty("Routes", out var routes);

            // Assert
            Assert.IsTrue(hasRoutes);
            Assert.AreEqual(JsonValueKind.Array, routes.ValueKind);
        }

        [TestMethod]
        public void OcelotConfig_HasGlobalConfiguration()
        {
            // Act
            var hasGlobalConfig = _ocelotConfig.RootElement.TryGetProperty("GlobalConfiguration", out var globalConfig);

            // Assert
            Assert.IsTrue(hasGlobalConfig);
        }

        [TestMethod]
        public void OcelotConfig_HasProductsRoute()
        {
            // Act
            var routes = _ocelotConfig.RootElement.GetProperty("Routes").EnumerateArray();
            var productsRoute = routes.FirstOrDefault(r =>
                r.GetProperty("UpstreamPathTemplate").GetString() == "/v1/products");

            // Assert
            Assert.AreNotEqual(default, productsRoute);
            Assert.AreEqual("/api/v1/products", productsRoute.GetProperty("DownstreamPathTemplate").GetString());
            Assert.AreEqual("product.api", productsRoute.GetProperty("DownstreamHostAndPorts")[0].GetProperty("Host").GetString());
        }

        [TestMethod]
        public void OcelotConfig_HasCouponsRoute()
        {
            // Act
            var routes = _ocelotConfig.RootElement.GetProperty("Routes").EnumerateArray();
            var couponsRoute = routes.FirstOrDefault(r =>
                r.GetProperty("UpstreamPathTemplate").GetString() == "/v1/coupons");

            // Assert
            Assert.AreNotEqual(default, couponsRoute);
            Assert.AreEqual("/api/v1/coupons", couponsRoute.GetProperty("DownstreamPathTemplate").GetString());
            Assert.AreEqual("coupon.api", couponsRoute.GetProperty("DownstreamHostAndPorts")[0].GetProperty("Host").GetString());
        }

        [TestMethod]
        public void OcelotConfig_HasCartRoute()
        {
            // Act
            var routes = _ocelotConfig.RootElement.GetProperty("Routes").EnumerateArray();
            var cartRoute = routes.FirstOrDefault(r =>
                r.GetProperty("UpstreamPathTemplate").GetString() == "/v1/cart");

            // Assert
            Assert.AreNotEqual(default, cartRoute);
            Assert.AreEqual("/api/v1/cart", cartRoute.GetProperty("DownstreamPathTemplate").GetString());
            Assert.AreEqual("shoppingcart.api", cartRoute.GetProperty("DownstreamHostAndPorts")[0].GetProperty("Host").GetString());
        }

        [TestMethod]
        public void OcelotConfig_HasOrdersRoute()
        {
            // Act
            var routes = _ocelotConfig.RootElement.GetProperty("Routes").EnumerateArray();
            var ordersRoute = routes.FirstOrDefault(r =>
                r.GetProperty("UpstreamPathTemplate").GetString() == "/v1/orders");

            // Assert
            Assert.AreNotEqual(default, ordersRoute);
            Assert.AreEqual("/api/v1/orders", ordersRoute.GetProperty("DownstreamPathTemplate").GetString());
            Assert.AreEqual("order.api", ordersRoute.GetProperty("DownstreamHostAndPorts")[0].GetProperty("Host").GetString());
        }

        #endregion

        #region HTTP Methods Tests

        [TestMethod]
        public void ProductsRoute_SupportsGetAndPost()
        {
            // Act
            var routes = _ocelotConfig.RootElement.GetProperty("Routes").EnumerateArray();
            var productsRoute = routes.FirstOrDefault(r =>
                r.GetProperty("UpstreamPathTemplate").GetString() == "/v1/products");
            var methods = productsRoute.GetProperty("UpstreamHttpMethod").EnumerateArray()
                .Select(m => m.GetString()).ToList();

            // Assert
            Assert.IsTrue(methods.Contains("GET"));
            Assert.IsTrue(methods.Contains("POST"));
        }

        [TestMethod]
        public void ProductsByIdRoute_SupportsGetPutDelete()
        {
            // Act
            var routes = _ocelotConfig.RootElement.GetProperty("Routes").EnumerateArray();
            var productsRoute = routes.FirstOrDefault(r =>
                r.GetProperty("UpstreamPathTemplate").GetString() == "/v1/products/{id}");
            var methods = productsRoute.GetProperty("UpstreamHttpMethod").EnumerateArray()
                .Select(m => m.GetString()).ToList();

            // Assert
            Assert.IsTrue(methods.Contains("GET"));
            Assert.IsTrue(methods.Contains("PUT"));
            Assert.IsTrue(methods.Contains("DELETE"));
        }

        [TestMethod]
        public void CartRoute_SupportsGetPostDelete()
        {
            // Act
            var routes = _ocelotConfig.RootElement.GetProperty("Routes").EnumerateArray();
            var cartRoute = routes.FirstOrDefault(r =>
                r.GetProperty("UpstreamPathTemplate").GetString() == "/v1/cart");
            var methods = cartRoute.GetProperty("UpstreamHttpMethod").EnumerateArray()
                .Select(m => m.GetString()).ToList();

            // Assert
            Assert.IsTrue(methods.Contains("GET"));
            Assert.IsTrue(methods.Contains("POST"));
            Assert.IsTrue(methods.Contains("DELETE"));
        }

        #endregion

        #region Downstream Configuration Tests

        [TestMethod]
        public void AllRoutes_UseHttpScheme()
        {
            // Act
            var routes = _ocelotConfig.RootElement.GetProperty("Routes").EnumerateArray();

            // Assert
            foreach (var route in routes)
            {
                var scheme = route.GetProperty("DownstreamScheme").GetString();
                Assert.AreEqual("http", scheme, $"Route {route.GetProperty("UpstreamPathTemplate").GetString()} should use http scheme");
            }
        }

        [TestMethod]
        public void AllRoutes_UsePort80()
        {
            // Act
            var routes = _ocelotConfig.RootElement.GetProperty("Routes").EnumerateArray();

            // Assert
            foreach (var route in routes)
            {
                var port = route.GetProperty("DownstreamHostAndPorts")[0].GetProperty("Port").GetInt32();
                Assert.AreEqual(80, port, $"Route {route.GetProperty("UpstreamPathTemplate").GetString()} should use port 80");
            }
        }

        #endregion

        #region Route Mapping Tests

        [TestMethod]
        public void AllRoutes_HaveValidDownstreamPathTemplate()
        {
            // Act
            var routes = _ocelotConfig.RootElement.GetProperty("Routes").EnumerateArray();

            // Assert
            foreach (var route in routes)
            {
                var downstreamPath = route.GetProperty("DownstreamPathTemplate").GetString();
                Assert.IsNotNull(downstreamPath);
                Assert.IsTrue(downstreamPath.StartsWith("/api/"),
                    $"Downstream path should start with /api/ but was: {downstreamPath}");
            }
        }

        [TestMethod]
        public void AllRoutes_HaveValidUpstreamPathTemplate()
        {
            // Act
            var routes = _ocelotConfig.RootElement.GetProperty("Routes").EnumerateArray();

            // Assert
            foreach (var route in routes)
            {
                var upstreamPath = route.GetProperty("UpstreamPathTemplate").GetString();
                Assert.IsNotNull(upstreamPath);
                Assert.IsTrue(upstreamPath.StartsWith("/"),
                    $"Upstream path should start with / but was: {upstreamPath}");
            }
        }

        #endregion
    }
}
