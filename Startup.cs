using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using BlazorTrader.Data;
using BlazorStrap;
using Syncfusion.Blazor;
// other usings
using Blazorise;
using Blazorise.Bootstrap;
using Blazorise.Icons.FontAwesome;

using Microsoft.EntityFrameworkCore;

namespace BlazorTrader
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddBlazorise(options =>
           { options.ChangeTextOnKeyPress = true; }).AddBootstrapProviders().AddFontAwesomeIcons();
            // other services           
            services.AddRazorPages();
            services.AddServerSideBlazor();
            services.AddTransient<WeatherForecastService>();
            services.AddTransient<TDAApiService>();
            services.AddTransient<OptionQuotesMgr>();
            services.AddTransient<ViewDataService>();
            services.AddTransient<TDAOptionsTableManager>();
            services.AddScoped<BlazorTimer>();
            services.AddScoped<Radzen.DialogService>();
            services.AddBootstrapCss();
            services.AddSyncfusionBlazor();
            services.AddDbContext<SqlDbContext>(options=>
              options.UseSqlServer(Configuration.GetConnectionString("TDAStreamerDb")));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("MjU4NjU2QDMxMzgyZTMxMmUzMFlwWGZWd05tUWxlVjkvdnVsM2tycThCdE5OQVROOGIzaCs4d2d1WjFpeTg9;MjU4NjU3QDMxMzgyZTMxMmUzMGNtdjZZMG1wYlAvdTUzYlZBUDFTdUhDRzlXQVlER3ErSFgrNjM0QzNhOTA9");
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
            app.ApplicationServices.UseBootstrapProviders().UseFontAwesomeIcons();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });
        }
    }
}
