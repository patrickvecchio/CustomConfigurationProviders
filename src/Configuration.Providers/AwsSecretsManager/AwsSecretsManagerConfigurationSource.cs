using Amazon.SecretsManager;
using Microsoft.Extensions.Configuration;

namespace Configuration.Providers.AwsSecretsManager
{
    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public class AwsSecretsManagerConfigurationSource : IConfigurationSource
    {
        private readonly IConfigurationRoot _configurationRoot;
        private readonly string _secretKeyName;
        private readonly IAmazonSecretsManager _amazonSecretsManager;

        /// <summary>
        /// Sets up AWS Secrets Manager as a configuration source.
        /// </summary>
        /// <param name="configurationRoot"><see cref="IConfigurationRoot"/></param>
        /// <param name="secretKeyName">The key name that we'll search for to find AWS Secrets Manager keys and replace with values from AWS Secrets Manager.</param>
        public AwsSecretsManagerConfigurationSource(IConfigurationRoot configurationRoot, IAmazonSecretsManager amazonSecretsManager, string secretKeyName)
        {
            _configurationRoot = configurationRoot;
            _secretKeyName = secretKeyName;
            _amazonSecretsManager = amazonSecretsManager;
        }

        public IConfigurationProvider Build(IConfigurationBuilder builder) =>
            new AwsSecretsManagerConfigurationProvider(_configurationRoot, _amazonSecretsManager, _secretKeyName);
    }
}
