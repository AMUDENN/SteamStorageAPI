using System.Reflection;
using AdminPanel.Utilities;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using SteamStorageAPI.SDK.Services.Logger.LoggerService;
using SteamStorageAPI.SDK.Utilities.Extensions.ServiceCollection;
using LoggerService = AdminPanel.Services.LoggerService.LoggerService;

namespace AdminPanel;

public static class Program
{
    #region Methods

    private static WebApplicationBuilder ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddControllersWithViews();

        //SteamStorageApi
        builder.Services.AddSteamStorageApiWeb(options =>
        {
            options.ClientTimeout = ProgramConstants.API_CLIENT_TIMEOUT;
        });

        //Custom SteamStorageApi Services
        builder.Services.AddSingleton<ILoggerService, LoggerService>();
        
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
            app.UseExceptionHandler("/Authorize/Error");
            app.UseHsts();
        }

        //ForwardedHeaders
        app.UseForwardedHeaders(new()
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
            name: "default",
            pattern: "admin/{controller=Authorize}/{action=Index}/{id?}");

        app.Run();
    }

    #endregion Methods
}
