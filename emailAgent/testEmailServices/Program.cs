using Microsoft.Extensions.Configuration;
using Serilog;
using testEmailServices;

// Setup configuration by loading settings.json and environment variables
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("settings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .CreateLogger();

try
{
    Log.Information("Starting testEmailServices application");
    
    // Create and run the test
    var testEmailServices = new TestEmailServices(configuration);
    await testEmailServices.Run();
    
    Log.Information("testEmailServices application completed successfully");
}
catch (Exception ex)
{
    Log.Fatal(ex, "testEmailServices application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
