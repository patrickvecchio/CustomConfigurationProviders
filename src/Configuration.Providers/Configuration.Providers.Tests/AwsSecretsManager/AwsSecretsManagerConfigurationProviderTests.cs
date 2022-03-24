using Configuration.Providers.Tests.AwsSecretsManager.Mocks;
using Configuration.Providers.Tests.AwsSecretsManager.Options;
using Extensions.Configuration.Providers.AwsSecretsManager;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace Configuration.Providers.Tests.AwsSecretsManager
{
    [TestClass]
    public class AwsSecretsManagerConfigurationProviderTests
    {
        public AwsSecretsManagerConfigurationProviderTests()
        {
        }

        [TestMethod]
        public void AwsSecretsConfigurationProvider_Generates_ExpectedValues()
        {
            // setup the secrets that the mock AWS Secrets Manager will return
            var mockSecretsData = new Dictionary<string, object>
            {
                {
                    "secret1",
                    new TestOptions{
                        secretString = "Here is a secretstring",
                        secretInt = 13,
                        secretDouble = 0.15,
                        secretBool = true
                    }
                }
            };

            var appSettings = new Dictionary<string, string>
            {
                { "someSettings:AwsSecret", "secret1" }
            };

            SetupTestEnvironment(appSettings, mockSecretsData, out var configuration, out var serviceProvider);

            var testOptions = serviceProvider.GetRequiredService<IOptions<TestOptions>>().Value;

            var mockTestOptions = (TestOptions)mockSecretsData["secret1"];

            Assert.AreEqual<string>(mockTestOptions.secretString, testOptions.secretString);
            Assert.AreEqual<int>(mockTestOptions.secretInt, testOptions.secretInt);
            Assert.AreEqual<double>(mockTestOptions.secretDouble, testOptions.secretDouble);
            Assert.AreEqual<bool>(mockTestOptions.secretBool, testOptions.secretBool);

            Assert.AreEqual<string>(mockTestOptions.secretString, configuration.GetValue<string>("someSettings:secretString"));
            Assert.AreEqual<int>(mockTestOptions.secretInt, configuration.GetValue<int>("someSettings:secretInt"));
            Assert.AreEqual<double>(mockTestOptions.secretDouble, configuration.GetValue<double>("someSettings:secretDouble"));
            Assert.AreEqual<bool>(mockTestOptions.secretBool, configuration.GetValue<bool>("someSettings:secretBool"));
        }

        private void SetupTestEnvironment(Dictionary<string,string> appSettings, Dictionary<string, object> mockSecretsData, out IConfigurationRoot configuration, out IServiceProvider serviceProvider)
        {
            configuration = new ConfigurationBuilder()
                            .AddInMemoryCollection(appSettings)
                            .AddAwsSecretsManager(new MockAwsSecretsManager(mockSecretsData), "AwsSecret")
                            .Build();

            var services = new ServiceCollection();

            services.AddOptions<TestOptions>()
                .Configure<IConfiguration>((settings, configuration) =>
                {
                    configuration.GetSection("someSettings").Bind(settings);
                });

            serviceProvider = services.BuildServiceProvider();
        }
    }
}
