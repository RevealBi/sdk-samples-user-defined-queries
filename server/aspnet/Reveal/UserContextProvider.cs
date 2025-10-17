using Microsoft.Extensions.Options;
using Reveal.Sdk;
using RevealSdk.Server.Configuration;

namespace RevealSdk.Server.Reveal
{
    public class UserContextProvider : IRVUserContextProvider
    {
        private readonly ServerOptions _options;

        public UserContextProvider(IOptions<ServerOptions> sqlOptions)
        {
            _options = sqlOptions.Value;
        }

        IRVUserContext IRVUserContextProvider.GetUserContext(HttpContext aspnetContext)
        {
            // string? headerValue = aspnetContext.Request.Headers["x-header-guid"].FirstOrDefault();
            // string? guid = headerValue;
            string? userId = "defaultUser";

            var props = new Dictionary<string, object>() {
                // { "Guid", guid ?? string.Empty },
                { "UserId", userId ?? string.Empty },
                { "Database", _options.Database },
                { "Username", _options.Username },
                { "Password", _options.Password },
                { "Schema", _options.Schema },
                { "Port", _options.Port },
                { "Host", _options.Host }
            };

            return new RVUserContext(userId, props);
        }
    }
}