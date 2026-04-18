using AdminPanel.Utilities;
using Microsoft.AspNetCore.HttpOverrides;
using SteamStorageAPI.SDK.Utilities.Extensions.ServiceCollection.Api;

namespace AdminPanel;

public static class Program
{
    #region Methods

    private static WebApplicationBuilder ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddControllersWithViews();

        //SteamStorageApi
        builder.Services.AddSteamStorageApiWeb(options => {
            options.ClientTimeout = ProgramConstants.API_CLIENT_TIMEOUT;
            options.ClientName = "MainClient";
            options.HostName = "localhost:5275";
            options.ServerAddress = "127.0.0.1:5275";
            options.ApiAddress = "http://localhost:5275/api";
            options.TokenHubEndpoint = "https://steamstorage.ru/token/token-hub";
        });

        builder.Services.AddHttpContextAccessor();

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