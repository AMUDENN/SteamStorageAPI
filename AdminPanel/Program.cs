using AdminPanel.Models;
using AdminPanel.Services.CookiesUserService;
using AdminPanel.Utilities.Config;
using Microsoft.AspNetCore.HttpOverrides;
using SteamStorageAPI.SDK.Utilities.Extensions.ServiceCollection.Api;

namespace AdminPanel;

public static class Program
{
    #region Methods

    private static WebApplicationBuilder ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddControllersWithViews();

        //Initialize config
        string? configPath = Environment.GetEnvironmentVariable("CONFIG_PATH");
        if (configPath is null) throw new Exception("Path to configuration file not set");

        AppConfig config = ConfigurationReader.Read(configPath);
        builder.Services.AddSingleton(config);

        //SteamStorageApi
        builder.Services.AddSteamStorageApiWeb(options => {
            options.ClientTimeout = config.App.ClientTimeout;
            options.ClientName = config.App.ClientName;
            options.HostName = config.App.HostName;
            options.ServerAddress = config.App.ServerAddress;
            options.ApiAddress = config.App.ApiAddress;
            options.TokenHubEndpoint = config.App.TokenHubEndpoint;
        });

        builder.Services.AddSingleton(new AdminPanelOptions
        {
            ApiAddress = config.App.ApiAddress
        });
        builder.Services.AddHttpClient();
        builder.Services.AddHttpContextAccessor();

        //Services
        builder.Services.AddTransient<ICookiesUserService, CookiesUserService>();

        return builder;
    }

    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = ConfigureServices(WebApplication.CreateBuilder(args));

        WebApplication app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error/Error");
            app.UseHsts();
        }

        //ForwardedHeaders
        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        });

        //app.UseHttpsRedirection();
        app.UseStaticFiles(new StaticFileOptions
        {
            RequestPath = "/admin"
        });

        app.UseRouting();

        app.UseAuthorization();

        app.MapControllerRoute(
            "default",
            "admin/{controller=Authorize}/{action=Index}/{id?}");

        app.Run();
    }

    #endregion Methods
}