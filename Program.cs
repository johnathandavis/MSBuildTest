using Microsoft.Extensions.Logging;

namespace MSBuildTest
{
    class Program
    {
        private const string PROJECT_PATH = "/Users/john/RiderProjects/PackageWithNuget/PackageWithNuget/PackageWithNuget.csproj";
        static void Main(string[] args)
        {
            var loggerFactory = CreateLoggerFactory();
            var builder = new ProjectBuilder(PROJECT_PATH, loggerFactory.CreateLogger<ProjectBuilder>());
            builder.Build();
        }

        private static ILoggerFactory CreateLoggerFactory() => LoggerFactory.Create(builder =>
        {
            builder.AddFilter("Microsoft", LogLevel.Warning)
                .AddFilter("System", LogLevel.Warning)
                .AddFilter("MSBuildTest", LogLevel.Debug)
                .AddConsole();
        });
    }
}