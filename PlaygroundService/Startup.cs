using ProofService.interfaces;
using Serilog;

namespace ProofService;

public class Startup
{
    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        var logger = services.BuildServiceProvider().GetRequiredService<ILogger<Startup>>();
        services.AddSingleton(logger);
        var configuration = builder.Build();
        var contractSetting = configuration.GetSection("ContractSetting").Get<ContractSetting>();
        
        // Dependency injection
        services.AddSingleton(contractSetting);

        // Add framework services.
        services.AddControllers();
        
        services.AddLogging(logging => logging.AddSerilog());
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseAuthorization();
        app.UseStaticFiles();
        app.UseDirectoryBrowser();
        app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
    }
}