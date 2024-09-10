using System;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace Custom
{
    static class CustomConfigurationManager
    {
        public static IConfiguration AppSetting { get; }
        static CustomConfigurationManager()
        {
            AppSetting = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
        }
    }
}
