using System;
using System.Configuration;

namespace ZavaAlertService
{
    internal static class DbConfig
    {
        public static string GetConnectionString()
        {
            var host = GetEnvironmentValue("DB_HOST", "DATABASE_HOST");
            var port = GetEnvironmentValue("DB_PORT", "DATABASE_PORT");
            var database = GetEnvironmentValue("DB_NAME", "DATABASE_NAME");
            var user = GetEnvironmentValue("DB_USER", "DATABASE_USER");
            var password = GetEnvironmentValue("DB_PASSWORD", "DATABASE_PASSWORD");

            if (!string.IsNullOrWhiteSpace(host) &&
                !string.IsNullOrWhiteSpace(port) &&
                !string.IsNullOrWhiteSpace(database) &&
                !string.IsNullOrWhiteSpace(user) &&
                !string.IsNullOrWhiteSpace(password))
            {
                return string.Format(
                    "Server={0},{1};Database={2};User Id={3};Password={4};TrustServerCertificate=true;",
                    host,
                    port,
                    database,
                    user,
                    password);
            }

            var configured = ConfigurationManager.ConnectionStrings["ZavaBankDb"];
            return configured != null ? configured.ConnectionString : string.Empty;
        }

        private static string GetEnvironmentValue(string primaryKey, string fallbackKey)
        {
            var primary = Environment.GetEnvironmentVariable(primaryKey);
            if (!string.IsNullOrWhiteSpace(primary))
            {
                return primary;
            }

            return Environment.GetEnvironmentVariable(fallbackKey);
        }
    }
}
