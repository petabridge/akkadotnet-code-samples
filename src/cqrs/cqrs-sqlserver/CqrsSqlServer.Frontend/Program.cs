using CqrsSqlServer.DataModel;
using CqrsSqlServer.Frontend.Components;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;

namespace CqrsSqlServer.Frontend;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

		builder.Services.AddMudServices();
        
        var connectionString = builder.Configuration.GetConnectionString("AkkaSqlConnection");
        if (connectionString is null)
            throw new Exception("AkkaSqlConnection setting is missing");

        builder.Services.AddDbContext<CqrsSqlServerContext>(options =>
        {
            // disable change tracking for all implementations of this context
            options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            options.UseSqlServer(connectionString);
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseStaticFiles();
        app.UseAntiforgery();

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.Run();
    }
}
