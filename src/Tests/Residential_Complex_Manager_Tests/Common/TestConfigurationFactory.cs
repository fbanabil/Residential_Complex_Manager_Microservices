using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;

namespace Residential_Complex_Manager_Tests.Common
{
    /// <summary>
    /// Shared helpers for tests. Builds in-memory configuration with a pepper for password
    /// hashing and a freshly generated RSA private key for JWT signing.
    /// </summary>
    public static class TestConfigurationFactory
    {
        private static readonly Lazy<string> _privateKeyPem = new(() =>
        {
            using var rsa = RSA.Create(2048);
            var keyBytes = rsa.ExportPkcs8PrivateKey();
            return "-----BEGIN PRIVATE KEY-----\n" +
                   Convert.ToBase64String(keyBytes, Base64FormattingOptions.InsertLineBreaks) +
                   "\n-----END PRIVATE KEY-----";
        });

        public static IConfiguration BuildAuthConfiguration(string? pepper = "TEST_PEPPER")
        {
            var settings = new Dictionary<string, string?>
            {
                ["Security:PasswordPepper"] = pepper,
                ["JwtSettings:PrivateKey"] = _privateKeyPem.Value,
                ["JwtSettings:Issuer"] = "test-issuer",
                ["JwtSettings:Audience"] = "test-audience",
            };
            return new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        }

        public static string ValidBase64Png =>
            "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII=";

        public static string ValidBase64Jpg =>
            "data:image/jpeg;base64,/9j/4AAQSkZJRgABAQEAAAAAAAD/2wBDAAEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQH/2wBDAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQH/wAARCAABAAEDASIAAhEBAxEB/8QAFQABAQAAAAAAAAAAAAAAAAAAAAr/xAAUEAEAAAAAAAAAAAAAAAAAAAAA/9oADAMBAAIRAxEAPwA/8H//2Q==";
    }
}
