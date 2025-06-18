using System;
using System.IO;
using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Logging;
using Google.Apis.Services;

namespace test
{

    class MainClass
    {
        public static void Main(string[] args)
        {
            string keyVaultName = "kvGPSecrets";
            string outputFile = args.Length > 0 ? args[0] : "agenda.html";
            var settings = new Settings(keyVaultName);
            var agent = new CalendarAgent(settings, outputFile);
            agent.GetEvents();
        }
    }
}
