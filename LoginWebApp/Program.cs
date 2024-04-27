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
            app.UseHsts();
        }

        //ForwardedHeaders
        app.UseForwardedHeaders(new()
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        });

        //app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.MapHub<TokenHub>("/token/token-hub");

        app.UseRouting();

        app.MapControllerRoute(
            name: "default",
            pattern: "token/{action=Token}/{id?}");

        app.Run();
    }

    #endregion Methods
}
