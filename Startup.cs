using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ElasticAPI.Services.ElasticService.Indexes;
using ElasticAPI.Services.LogicService;
using ElasticAPI.Services.ElasticService.Filter;
using ElasticAPI.Services.ElasticService.Filter.Biopcy;

namespace ElasticAPI
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            // CORS politikasını ekle
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll",
                    builder =>
                    {
                        builder
                            .AllowAnyOrigin()
                            .AllowAnyMethod()
                            .AllowAnyHeader();
                    });
            });

            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "ElasticAPI", Version = "v1" });
            });

            // OrchestrateIndexes servisini kaydet
            services.AddScoped<OrchestrateIndexes>();
            services.AddScoped<IBiopcyFilter, BiopcyFilter>();
            services.AddScoped<IOrchestrateFilter, OrchestrateFilter>();
            services.AddScoped<IFilterLogic, FilterLogic>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "ElasticAPI v1"));
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            // CORS middleware'ini UseRouting ve UseAuthorization arasına ekle
            app.UseCors("AllowAll");

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}