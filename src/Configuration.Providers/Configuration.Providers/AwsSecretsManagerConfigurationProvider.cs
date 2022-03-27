using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Configuration.Providers.AwsSecretsManager
{
    /// <summary>
    /// The custom <see cref="Configuration"/> that will retrieve AWS Secrets Manager values.
    /// </summary>
    public class AwsSecretsManagerConfigurationProvider : ConfigurationProvider
    {
        private readonly IConfigurationRoot _configuration;
        private readonly string _secretKeyName;
        private readonly IAmazonSecretsManager _amazonSecretsManager;

        /// <summary>
        /// Creates an instance of <see cref="AwsSecretsManagerConfigurationProvider"/>
        /// </summary>
        /// <param name="configuration">The <see cref="IConfigurationRoot"/> that holds the config to be parsed.</param>
        /// <param name="secretKeyName">The key name that we'll search for to find AWS Secrets Manager keys and replace with values from AWS Secrets Manager.</param>
        public AwsSecretsManagerConfigurationProvider(IConfigurationRoot configuration, IAmazonSecretsManager amazonSecretsManager, string secretKeyName)
        {
            _configuration = configuration;
            _secretKeyName = secretKeyName;
            _amazonSecretsManager = amazonSecretsManager;
        }

        /// <inheritdoc/>
        public override void Load()
        {
            Data.Clear();
            var expandedKeyValuePairs = new List<KeyValuePair<string,string>>();
            RecurseForData(_configuration, _secretKeyName, ref expandedKeyValuePairs);

            foreach (var expandedKeyValuePair in expandedKeyValuePairs)
            {
                Data.Add(expandedKeyValuePair);
            }
        }

        /// <summary>
        /// Recusively search for the secret key name and extract a <see cref="List{T}"/> of <see cref="KeyValuePair"/>"/> containing the config data found.
        /// </summary>
        /// <param name="config">The <see cref="IConfiguration"/> to be searched.</param>
        /// <param name="secretKeyName">The key name to search for.</param>
        /// <param name="keyValuePairs">A <see cref="List{T}"/> of <see cref="KeyValuePair"/>"/> containing the config data found.</param>
        private void RecurseForData(IConfiguration config, string secretKeyName, ref List<KeyValuePair<string, string>> keyValuePairs)
        {
            foreach (var child in config.GetChildren())
            {
                if (0 == string.Compare(child.Key, secretKeyName, true))
                {
                    var secretValueJson = GetAwsSecret(child.Value);
                    var parentPath = child.Path.Replace($":{child.Key}", null, StringComparison.InvariantCultureIgnoreCase);
                    var secretValues = GetKeyValuePairsFromJson(parentPath, secretValueJson);
                    keyValuePairs.AddRange(secretValues);
                }
                else
                {
                    RecurseForData(child, secretKeyName, ref keyValuePairs);
                }
            }
        }

        /// <summary>
        /// Pulls <see cref="KeyValuePair"/> data from the JSON returned by 
        /// </summary>
        /// <param name="parentPath">The parent path in the <see cref="IConfigurationSection"/> being parsed.</param>
        /// <param name="json">The JSON contained within the current <see cref="IConfigurationSection"/> being parsed.</param>
        /// <returns></returns>
        private Dictionary<string, string> GetKeyValuePairsFromJson(string parentPath, string json)
        {
            JsonSerializerOptions jsonSerializationOptions = new JsonSerializerOptions
            {
                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var secretValues = JsonSerializer.Deserialize<Dictionary<string, string>>(json, jsonSerializationOptions);

            if (string.IsNullOrWhiteSpace(parentPath))
            {
                return secretValues;
            }

            var secretValuesWithParentPath = new Dictionary<string, string>();
            foreach (var secretValue in secretValues)
            {
                secretValuesWithParentPath.Add($"{parentPath}:{secretValue.Key}", secretValue.Value);
            }

            return secretValuesWithParentPath;
        }

        /// <summary>
        /// Retrieve secret from AWS Secrets Manager
        /// </summary>
        /// <param name="key">AWS Secrets Manager key to retrieve.</param>
        /// <returns>A string representing the value in AWS Secrets Manager.</returns>
        private string GetAwsSecret(string key)
        {
            GetSecretValueRequest request = new GetSecretValueRequest
            {
                SecretId = key
            };

            GetSecretValueResponse response = _amazonSecretsManager.GetSecretValueAsync(request).GetAwaiter().GetResult();

            if (response.SecretString == null)
            {
                var buffer = response.SecretBinary.GetBuffer();
                return Convert.ToBase64String(buffer);
            }
            else
            {
                return response.SecretString;
            }
        }
    }
}
