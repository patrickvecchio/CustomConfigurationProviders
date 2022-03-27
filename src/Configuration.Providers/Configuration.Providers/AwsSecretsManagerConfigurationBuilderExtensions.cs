using Amazon.SecretsManager;
using Microsoft.Extensions.Configuration;

namespace Configuration.Providers.AwsSecretsManager
{
    /// <summary>
    /// A class to contain the extension methods.
    /// </summary>
    public static class AwsSecretsManagerConfigurationBuilderExtensions
    {
        /// <summary>
        /// This is the extension method used to add <see cref="AwsSecretsManagerConfigurationProvider"/> to a <see cref="ConfigurationBuilder"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/> to extend.</param>
        /// <param name="secretKeyName">The key name that we'll search for to find AWS Secrets Manager keys and replace with values from AWS Secrets Manager.</param>
        /// <returns></returns>
        public static IConfigurationBuilder AddAwsSecretsManager(
            this IConfigurationBuilder builder, IAmazonSecretsManager amazonSecretsManager, string secretKeyName)
        {
            // since we're parsing and replacing existing config items, we need to temporarily build the config
            var tempConfig = builder.Build();

            return builder.Add(new AwsSecretsManagerConfigurationSource(tempConfig, amazonSecretsManager, secretKeyName));
        }
    }
}
