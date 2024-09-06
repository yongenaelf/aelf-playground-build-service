using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using System;
public class Startup
{

    public IConfiguration Configuration { get; }

    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        // Check if Configuration is properly loaded
        if (Configuration == null)
        {
            throw new Exception("Configuration is null. Ensure it is initialized correctly.");
        }

        var mongoClient = new MongoClient(Configuration["MongoDbSettings.ConnectionString"]); // DB String
        var mongoDatabase = mongoClient.GetDatabase(Configuration["MongoDbSettings.DatabaseName"]); // TabelName
        var gridFSBucket = new GridFSBucket(mongoDatabase);

        services.AddSingleton<IGridFSBucket>(gridFSBucket);

        // Add controllers to the services
        services.AddControllers();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        // app.UseHttpsRedirection();
        // app.UseStaticFiles();
        // app.UseDefaultFiles();
        app.UseRouting();

        // app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}