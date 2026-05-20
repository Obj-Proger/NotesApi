using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using DotNetEnv;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace NotesApi.Data
{
    /// <summary>
    /// Factory for creating DbContext instances at design-time for Entity Framework migrations.
    /// Loads environment variables from .env file and configures database connection.
    /// </summary>
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        private const string EnvironmentVariableAppEnv = "APP_ENV";
        private const string EnvironmentVariableDevelopment = "Development";
        private const string DotEnvFileName = ".env";

        /// <summary>
        /// Creates a configured AppDbContext instance for design-time operations.
        /// Loads connection string from environment variables or .env file.
        /// </summary>
        /// <param name="args">Command-line arguments (unused).</param>
        /// <returns>A fully configured AppDbContext instance.</returns>
        /// <exception cref="InvalidOperationException">Thrown when required environment variables are missing.</exception>
        public AppDbContext CreateDbContext(string[] args)
        {
            LoadEnvironmentVariables();

            var connectionString = BuildConnectionString();
            Console.WriteLine($"Using database: {Environment.GetEnvironmentVariable("DB_NAME")}");

            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseNpgsql(connectionString);

            ConfigureLoggingLevel(optionsBuilder);

            return new AppDbContext(optionsBuilder.Options);
        }

        /// <summary>
        /// Loads environment variables from .env file if it exists.
        /// Traverses from assembly location to find .env file in project root.
        /// </summary>
        private static void LoadEnvironmentVariables()
        {
            string envFilePath = LocateEnvFile();

            if (File.Exists(envFilePath))
            {
                Env.Load(envFilePath);
                Console.WriteLine($".env loaded from: {envFilePath}");
            }
            else
            {
                Console.WriteLine($"WARNING: .env not found at: {envFilePath}");
            }
        }

        /// <summary>
        /// Locates the .env file by traversing up from the assembly location to the project root.
        /// </summary>
        /// <returns>The full path to the expected .env file location.</returns>
        private static string LocateEnvFile()
        {
            string assemblyLocation = Assembly.GetExecutingAssembly().Location;
            string binFolder = Path.GetDirectoryName(assemblyLocation)!;
            string projectFolder = Path.GetFullPath(Path.Combine(binFolder, "..", "..", ".."));
            return Path.Combine(projectFolder, DotEnvFileName);
        }

        /// <summary>
        /// Constructs the PostgreSQL connection string from environment variables.
        /// </summary>
        /// <returns>The complete connection string for database access.</returns>
        /// <exception cref="InvalidOperationException">Thrown when required environment variable is missing.</exception>
        private static string BuildConnectionString()
        {
            // Retrieve and validate required database connection parameters.
            var host = GetRequiredEnvironmentVariable("DB_HOST");
            var port = GetRequiredEnvironmentVariable("DB_PORT");
            var database = GetRequiredEnvironmentVariable("DB_NAME");
            var username = GetRequiredEnvironmentVariable("DB_USER");
            var password = GetRequiredEnvironmentVariable("DB_PASSWORD");

            return $"Host={host};Port={port};Database={database};Username={username};Password={password}";
        }

        /// <summary>
        /// Retrieves a required environment variable and throws if not found.
        /// </summary>
        /// <param name="variableName">The name of the environment variable.</param>
        /// <returns>The value of the environment variable.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the variable is not set.</exception>
        private static string GetRequiredEnvironmentVariable(string variableName)
        {
            return Environment.GetEnvironmentVariable(variableName)
                ?? throw new InvalidOperationException($"{variableName} is not set.");
        }

        /// <summary>
        /// Configures Entity Framework Core logging based on the APP_ENV environment variable.
        /// Enables detailed logging and sensitive data output in Development mode only.
        /// </summary>
        /// <param name="optionsBuilder">The DbContextOptionsBuilder to configure.</param>
        private static void ConfigureLoggingLevel(DbContextOptionsBuilder<AppDbContext> optionsBuilder)
        {
            var appEnv = Environment.GetEnvironmentVariable(EnvironmentVariableAppEnv);
            bool isDevelopment = EnvironmentVariableDevelopment.Equals(appEnv, StringComparison.OrdinalIgnoreCase);

            if (isDevelopment)
            {
                // Enable verbose logging with SQL queries and sensitive data in Development.
                optionsBuilder
                    .LogTo(Console.WriteLine, LogLevel.Information)
                    .EnableSensitiveDataLogging()
                    .EnableDetailedErrors();
                Console.WriteLine("EF Core detailed logging: ENABLED (Development mode)");
            }
            else
            {
                Console.WriteLine("EF Core detailed logging: DISABLED (Production mode)");
            }
        }
    }
}