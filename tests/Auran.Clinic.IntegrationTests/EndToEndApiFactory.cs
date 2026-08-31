using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Auran.Clinic.IntegrationTests;

public sealed class EndToEndApiFactory : ApiFactory
{
    public const string PlatformAdminEmail = "platform-ci@example.com";
    public const string PlatformAdminPassword = "Auran_CI_Platform_Admin_123!";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PlatformBootstrap:Enabled"] = "true",
                ["PlatformBootstrap:FullName"] = "AURAN CI Platform Admin",
                ["PlatformBootstrap:Email"] = PlatformAdminEmail,
                ["PlatformBootstrap:Password"] = PlatformAdminPassword,
                ["PlatformBootstrap:Phone"] = "+201000000000"
            });
        });
    }
}
