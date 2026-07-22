using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using CongoTravel.Models.DTOs.Client;
using System.Net;
using Xunit;

namespace CongoTravel.Tests
{
    public class ClientRegistrationRateLimitAttributeTests
    {
        [Fact]
        public void Blocks_after_limit_for_same_email_scope()
        {
            var services = BuildServices(new ClientRegistrationRateLimitOptions
            {
                EmailLimit = 3,
                EmailWindowMinutes = 10,
                EnableDeviceScope = false,
                IpLimit = 100
            });
            var filter = new ClientRegistrationRateLimitAttribute();

            Execute(filter, services, "1.1.1.1", "mail@test.com");
            Execute(filter, services, "1.1.1.1", "mail@test.com");
            Execute(filter, services, "1.1.1.1", "mail@test.com");
            var blocked = Execute(filter, services, "1.1.1.1", "mail@test.com");

            Assert.NotNull(blocked.Result);
            Assert.Equal(StatusCodes.Status429TooManyRequests, ((Microsoft.AspNetCore.Mvc.JsonResult)blocked.Result!).StatusCode);
        }

        [Fact]
        public void Does_not_block_different_emails_from_same_ip_when_ip_not_flooded()
        {
            var services = BuildServices(new ClientRegistrationRateLimitOptions
            {
                EmailLimit = 3,
                EmailWindowMinutes = 10,
                EnableDeviceScope = false,
                IpLimit = 100
            });
            var filter = new ClientRegistrationRateLimitAttribute();

            var a = Execute(filter, services, "2.2.2.2", "a@test.com");
            var b = Execute(filter, services, "2.2.2.2", "b@test.com");
            var c = Execute(filter, services, "2.2.2.2", "c@test.com");

            Assert.Null(a.Result);
            Assert.Null(b.Result);
            Assert.Null(c.Result);
        }

        [Fact]
        public void Blocks_by_device_scope_when_enabled()
        {
            var services = BuildServices(new ClientRegistrationRateLimitOptions
            {
                EmailLimit = 100,
                EnableDeviceScope = true,
                DeviceLimit = 2,
                DeviceWindowMinutes = 10,
                IpLimit = 100
            });
            var filter = new ClientRegistrationRateLimitAttribute();

            Execute(filter, services, "3.3.3.3", "a@test.com", "device-1");
            Execute(filter, services, "3.3.3.3", "b@test.com", "device-1");
            var blocked = Execute(filter, services, "3.3.3.3", "c@test.com", "device-1");

            Assert.NotNull(blocked.Result);
            Assert.Equal(StatusCodes.Status429TooManyRequests, ((Microsoft.AspNetCore.Mvc.JsonResult)blocked.Result!).StatusCode);
        }

        private static ActionExecutingContext Execute(
            ClientRegistrationRateLimitAttribute filter,
            ServiceProvider services,
            string ip,
            string? email,
            string? deviceId = null)
        {
            var httpContext = new DefaultHttpContext
            {
                RequestServices = services,
                Connection = { RemoteIpAddress = IPAddress.Parse(ip) }
            };

            if (!string.IsNullOrWhiteSpace(deviceId))
                httpContext.Request.Headers["X-Device-Id"] = deviceId;

            var actionContext = new Microsoft.AspNetCore.Mvc.ActionContext(
                httpContext,
                new RouteData(),
                new ActionDescriptor { DisplayName = "ClientController.RegisterClient" });

            var args = new Dictionary<string, object?>
            {
                ["dto"] = new RegisterClientDto
                {
                    NomClient = "Test",
                    Telephone = "+2430000000",
                    EmailClient = email,
                    AcceptTerms = true
                }
            };

            var executing = new ActionExecutingContext(
                actionContext,
                new List<IFilterMetadata>(),
                args!,
                controller: null);

            filter.OnActionExecuting(executing);
            return executing;
        }

        private static ServiceProvider BuildServices(ClientRegistrationRateLimitOptions options)
        {
            var services = new ServiceCollection();
            services.AddMemoryCache();
            services.AddSingleton<IOptions<ClientRegistrationRateLimitOptions>>(Options.Create(options));
            services.AddSingleton<ILogger<ClientRegistrationRateLimitAttribute>>(
                Microsoft.Extensions.Logging.Abstractions.NullLogger<ClientRegistrationRateLimitAttribute>.Instance);
            return services.BuildServiceProvider();
        }
    }
}
