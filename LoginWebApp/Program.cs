using LoginWebApp.Utilities.Config;
using LoginWebApp.Utilities.TokenHub;
using Microsoft.AspNetCore.HttpOverrides;

namespace LoginWebApp;

public static class Program
{
    #region Methods

    private static WebApplicationBuilder ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddControllersWithViews();

        builder.Services.AddSignalR();

        //Initialize config
        string? configPath = Environment.GetEnvironmentVariable("CONFIG_PATH");
        if (configPath is null) throw new Exception("Path to configuration file not set");
        
        AppConfig config = ConfigurationReader.Read(configPath);
        builder.Services.AddSingleton(config);

        return builder;
    }

    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = ConfigureServices(WebApplication.CreateBuilder(args));

        WebApplication app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
            app.UseHsts();

        //ForwardedHeaders
        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        });

        //app.UseHttpsRedirection();
        app.UseStaticFiles(new StaticFileOptions
        {
            RequestPath = "/token"
        });

        app.MapHub<TokenHub>("/token/token-hub");

        app.UseRouting();

        app.MapControllerRoute(
            "default",
            "token/{action=Token}/{id?}");

        app.Run();
    }

    #endregion Methods
}