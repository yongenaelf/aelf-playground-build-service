using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using Custom;
public class Startup
{
    public Startup()
    {
    }

    public void ConfigureServices(IServiceCollection services)
    {
        string connectionString = CustomConfigurationManager.AppSetting["MongoDbSettings:ConnectionString"];
        string databaseName = CustomConfigurationManager.AppSetting["MongoDbSettings:DatabaseName"];

        var mongoClient = new MongoClient(connectionString);
        var mongoDatabase = mongoClient.GetDatabase(databaseName);
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