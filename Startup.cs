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
using tdaStreamHub.Data;
using BlazorStrap;
using Syncfusion.Blazor;
// other usings
using Blazorise;
using Blazorise.Bootstrap;
using Blazorise.Icons.FontAwesome;
using tdaStreamHub.Hubs;

using Microsoft.EntityFrameworkCore;

namespace tdaStreamHub
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
            services.AddScoped<tdaStreamHub.Data.BrowserService>(); // scoped service
            services.AddTransient<tdaStreamHub.Data.BlazorTimer>();

            services.AddBlazorise(options =>
           { options.ChangeTextOnKeyPress = true; }).AddBootstrapProviders().AddFontAwesomeIcons();
            // other services           
            services.AddRazorPages();
            services.AddServerSideBlazor();

            services.AddTransient<TDAApiService>();

            services.AddSignalR();
            services.AddResponseCompression(opts =>
            {
                opts.MimeTypes = Microsoft.AspNetCore.ResponseCompression.ResponseCompressionDefaults.MimeTypes.Concat(
                    new[] { "application/octet-stream" });
            });

            services.AddScoped<Radzen.DialogService>();
            services.AddBootstrapCss();

            services.AddCors();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseResponseCompression();

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

            // Make sure the CORS middleware is ahead of SignalR.
            app.UseCors(builder =>
            {
                builder.WithOrigins("http://localhost:53911")
                    .AllowAnyHeader()
                    .WithMethods("GET", "HEAD", "POST")
                    .AllowCredentials();
            });

            app.UseRouting();
            app.ApplicationServices.UseBootstrapProviders().UseFontAwesomeIcons();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapBlazorHub();
                endpoints.MapHub<ChatHub>("/chathub");
                endpoints.MapHub<TDAHub>("/tdahub");
                endpoints.MapFallbackToPage("/_Host");
            });
        }
    }
}
