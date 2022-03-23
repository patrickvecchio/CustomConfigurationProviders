using Microsoft.Extensions.Configuration;

namespace Extensions.Configuration.Providers.AwsSecretsManager
{
    public class AwsSecretsManagerConfigurationSource : IConfigurationSource
    {
        private readonly IConfigurationRoot _configurationRoot;
        private readonly string _secretKeyName;

        public AwsSecretsManagerConfigurationSource(IConfigurationRoot configurationRoot, string secretKeyName)
        {
            _configurationRoot = configurationRoot;
            _secretKeyName = secretKeyName;
        }

        public IConfigurationProvider Build(IConfigurationBuilder builder) =>
            new AwsSecretsManagerConfigurationProvider(_configurationRoot, _secretKeyName);
    }
}
