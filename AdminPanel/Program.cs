using AdminPanel.Models;
using AdminPanel.Services.CookiesUserService;
using Microsoft.AspNetCore.HttpOverrides;
using SteamStorageAPI.SDK.Utilities.Extensions.ServiceCollection.Api;

namespace AdminPanel;

public static class Program
{
    #region Methods

    private static WebApplicationBuilder ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddControllersWithViews();

        string apiInternalHost = builder.Configuration["STEAMSTORAGE_API_INTERNAL_HOST"] ?? "localhost:8081";
        string apiPublicHost = builder.Configuration["STEAMSTORAGE_API_PUBLIC_HOST"] ?? "localhost:8081";
        string apiAddress = $"http://{apiInternalHost}/api";

        //SteamStorageApi
        builder.Services.AddSteamStorageApiWeb(options => {
            options.ClientTimeout = 15;
            options.ClientName = "MainClient";
            options.HostName = apiPublicHost;
            options.ServerAddress = apiInternalHost;
            options.ApiAddress = apiAddress;
            options.TokenHubEndpoint = "https://steamstorage.ru/token/token-hub";
        });

        builder.Services.AddSingleton(new AdminPanelOptions
        {
            ApiAddress = apiAddress
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