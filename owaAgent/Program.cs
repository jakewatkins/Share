using Microsoft.Extensions.Configuration;
using EmailAgent;

var config = (IConfiguration) new ConfigurationBuilder()
                .AddJsonFile("settings.json")
                .AddEnvironmentVariables()
                .Build();

if (null == config)
{
    Console.WriteLine("failed to load config");
}

var settings = new Settings(config);


// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");
Console.WriteLine($"server url: {settings.ServerURI}");
