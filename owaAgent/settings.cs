using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace EmailAgent
{
    public class Settings
    {
        //private fields

        //constructors
        public Settings(IConfiguration configuration)
        {        
            EmailAddress = configuration.GetValue<string>("EmailAddress");
            Password = configuration.GetValue<string>("Password");
            ServerURI = configuration.GetValue<string>("ServerURI");
        }

        //properties
        public string EmailAddress { get; set; }
        public string Password { get; set; }
        public string ServerURI { get; set; }
    }
}