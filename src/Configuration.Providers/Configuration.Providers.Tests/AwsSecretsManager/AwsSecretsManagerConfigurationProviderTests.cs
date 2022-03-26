using Configuration.Providers.Tests.AwsSecretsManager.Mocks;
using Configuration.Providers.Tests.AwsSecretsManager.Options;
using Extensions.Configuration.Providers.AwsSecretsManager;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Reflection;
namespace Configuration.Providers.Tests.AwsSecretsManager
{
    [TestClass]
    public class AwsSecretsManagerConfigurationProviderTests
    {
        [TestMethod]
        public void AwsSecretsConfigurationProvider_Generates_ExpectedValues()
        {
            const string awsSecretManagerKey = "/dev/db/options";
            const string secretKeyName = "AwsSecret";

            // setup the secrets that the mock AWS Secrets Manager will return Dictionary of key name and JSON
            var mockSecretsData = new Dictionary<string, Dictionary<string, string>>
            {
                {
                    awsSecretManagerKey,
                    new Dictionary<string,string>
                    {
                        { "server", "localhost" },
                        { "port", "3306" },
                        { "Timeout", "5" },
                        { "UserName", "MyLocalAdmin" },
                        { "Password","StillNunyaBizness!" }
                    }
                }
            };

            // setup in-memory appsettings
            var sectionName = "somesettings";
            var appSettings = new Dictionary<string, string>
            {
                { $"{sectionName}:{secretKeyName}", awsSecretManagerKey }
            };

            SetupTestEnvironment<TestOptions>(
                sectionName,
                secretKeyName,
                appSettings,
                mockSecretsData,
                out var configuration,
                out var serviceProvider);

            AssertMockSecretsInConfiguration<TestOptions>(sectionName, awsSecretManagerKey, mockSecretsData, configuration);

            AssertMockSecretsInIOptions<TestOptions>(sectionName, awsSecretManagerKey, mockSecretsData, configuration, serviceProvider);
        }

        [TestMethod]
        public void AwsSecretsConfigurationProviderWithNestedProperties_Generates_ExpectedValues()
        {
            const string awsSecretManagerKey = "/dev/db/options";
            const string secretKeyName = "AwsSecret";

            // setup the secrets that the mock AWS Secrets Manager will return Dictionary of key name and JSON
            var mockSecretsData = new Dictionary<string, Dictionary<string, string>>
            {
                {
                    awsSecretManagerKey,
                    new Dictionary<string,string>
                    {
                        { "server", "localhost" },
                        { "port", "3306" },
                        { "Timeout", "5" },
                        { "UserName", "MyLocalAdmin" },
                        { "Password","StillNunyaBizness!" }
                    }
                }
            };

            // setup in-memory appsettings
            var sectionName = "somesettings:somesettings:evenmoresettings";
            var appSettings = new Dictionary<string, string>
            {
                { $"{sectionName}:{secretKeyName}", awsSecretManagerKey }
            };

            SetupTestEnvironment<TestOptions>(
                sectionName,
                secretKeyName,
                appSettings,
                mockSecretsData,
                out var configuration,
                out var serviceProvider);

            AssertMockSecretsInConfiguration<TestOptions>(sectionName, awsSecretManagerKey, mockSecretsData, configuration);

            AssertMockSecretsInIOptions<TestOptions>(sectionName, awsSecretManagerKey, mockSecretsData, configuration, serviceProvider);
        }

        private void SetupTestEnvironment<T>(
            string sectionName,
            string secretKeyName,
            Dictionary<string,string> appSettings,
            Dictionary<string, Dictionary<string, string>> mockSecretsData,
            out IConfigurationRoot configuration,
            out IServiceProvider serviceProvider) where T : class
        {
            configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(appSettings)
                .AddAwsSecretsManager(new MockAwsSecretsManager(mockSecretsData), secretKeyName)
                .Build();

            var services = new ServiceCollection();

            services.AddSingleton<IConfiguration>(configuration);

            services.AddOptions<T>()
                .Configure<IConfiguration>((settings, configuration) =>
                {
                    configuration.GetSection(sectionName).Bind(settings);
                });

            serviceProvider = services.BuildServiceProvider();
        }

        private void AssertMockSecretsInConfiguration<T>(
            string sectionName,
            string awsSecretManagerKey,
            Dictionary<string, Dictionary<string, string>> mockSecretsData,
            IConfigurationRoot configuration) where T : class, new()
        {
            var secrets = mockSecretsData[awsSecretManagerKey];
            foreach (var secret in secrets)
            {
                Assert.AreEqual(
                    secret.Value,
                    configuration[$"{sectionName}:{secret.Key}"],
                    $"Expected value (Mock data['{secret.Key}'] = '{secret.Value}') does not equal actual value" +
                    $" (configuration['{sectionName}:{secret.Key}'] = '{configuration[$"{sectionName}:{secret.Key}"]}')");
            }
        }


        private void AssertMockSecretsInIOptions<T>(
            string sectionName,
            string awsSecretManagerKey,
            Dictionary<string, Dictionary<string, string>> mockSecretsData,
            IConfigurationRoot configuration,
            IServiceProvider serviceProvider) where T : class, new()
        {
            var secrets = mockSecretsData[awsSecretManagerKey];
            var testOptions = serviceProvider.GetRequiredService<IOptions<T>>().Value;
            foreach (var secret in secrets)
            {
                var bindingFlags = BindingFlags.Public
                    | BindingFlags.Static
                    | BindingFlags.Instance
                    | BindingFlags.IgnoreCase;
                Assert.AreEqual(
                    secret.Value,
                    testOptions.GetType().GetProperty(secret.Key, bindingFlags).GetValue(testOptions),
                    $"Expected value (Mock data['{secret.Key}'] = '{secret.Value}') does not equal actual value" +
                    $" '{testOptions.GetType().Name}.{secret.Key}' = '{testOptions.GetType().GetProperty(secret.Key, bindingFlags).GetValue(testOptions)}'");
            }
        }
    }
}
