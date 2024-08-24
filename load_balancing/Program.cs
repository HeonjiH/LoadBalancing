using load_balancing.Components;
using load_balancing.Service;
using Microsoft.Extensions.DependencyInjection;
using System.Xml;

namespace load_balancing
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            builder.Services.AddHttpClient();

            // 외부 요청 용 프록시 서버 url
            var proxy1 = "http://localhost:5001";
            var proxy2 = "http://localhost:8080";
            string[] proxies = [proxy1, proxy2];
            builder.Services.AddSingleton<ProxyManager>(provider =>
            {
                return new ProxyManager(proxies);
            });
            builder.Services.AddTransient<ProxyHttpClientHandler>();
            // send
            builder.Services.AddHttpClient("ProxyClient")
                .ConfigureHttpClient(client => client.BaseAddress = new Uri("http://localhost:7278/"))
                .AddHttpMessageHandler<ProxyHttpClientHandler>();

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
}
