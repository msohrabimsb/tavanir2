using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using tavanir2.Models;

namespace tavanir2
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            services.Configure<CookiePolicyOptions>(options =>
            {
                options.CheckConsentNeeded = context => false; // افزودن تصدیق اعمال کوکی ها به صورت پیش فرض در مرورگر کاربر - بدون نیاز به تأیید کاربر
                options.Secure = CookieSecurePolicy.None;
                options.MinimumSameSitePolicy = SameSiteMode.Unspecified;
            });

            services.AddSession(s =>
            {
                s.IdleTimeout = TimeSpan.FromDays(365);
                s.Cookie.Name = "BI_TAVANIR_ARIAN";
                s.Cookie.SameSite = SameSiteMode.Unspecified;
                s.Cookie.SecurePolicy = CookieSecurePolicy.None;
                s.Cookie.MaxAge = TimeSpan.FromDays(365);
                s.Cookie.IsEssential = true; // make the session cookie Essential
            });

            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie();

            services.AddHttpContextAccessor();

            services.AddMvc();

            services.AddTransient<IBaseRepository, BaseRepository>();
            services.AddTransient<HashingPassword>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseCookiePolicy();
            app.UseAuthentication();
            app.UseSession();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
