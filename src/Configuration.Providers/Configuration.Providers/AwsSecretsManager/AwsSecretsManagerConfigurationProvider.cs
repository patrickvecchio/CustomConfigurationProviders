using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Extensions.Configuration.Providers.AwsSecretsManager
{
    /// <summary>
    /// The custom <see cref="Configuration"/> that will retrieve AWS Secrets Manager values.
    /// </summary>
    public class AwsSecretsManagerConfigurationProvider : ConfigurationProvider
    {
        private readonly IConfigurationRoot _configuration;
        private readonly string _secretKeyName;

        /// <summary>
        /// Creates an instance of <see cref="AwsSecretsManagerConfigurationProvider"/>
        /// </summary>
        /// <param name="configuration">The <see cref="IConfigurationRoot"/> that holds the config to be parsed.</param>
        /// <param name="secretKeyName">The key name that we'll search for to find AWS Secrets Manager keys and replace with values from AWS Secrets Manager.</param>
        public AwsSecretsManagerConfigurationProvider(IConfigurationRoot configuration, string secretKeyName)
        {
            _configuration = configuration;
            _secretKeyName = secretKeyName;
        }

        /// <inheritdoc/>
        public override void Load()
        {
            Data.Clear();
            //var expandedKeyValuePairs = ExtractData(_configuration, _secretKeyName);
            var expandedKeyValuePairs = new List<KeyValuePair<string,string>>();
            RecurseForData(_configuration, _secretKeyName, ref expandedKeyValuePairs);

            foreach (var expandedKeyValuePair in expandedKeyValuePairs)
            {
                Data.Add(expandedKeyValuePair);
            }
        }

        /// <summary>
        /// Extracts the AWS Secret Manager values for the given secret key name.
        /// </summary>
        /// <param name="config">The <see cref="IConfigurationRoot"/> to search.</param>
        /// <param name="secretKeyName">The key name to search for.</param>
        /// <returns></returns>
        private List<KeyValuePair<string, string>> ExtractData(IConfigurationRoot config, string secretKeyName)
        {
            var keyValuePairs = new List<KeyValuePair<string, string>>();
            RecurseForData(config, secretKeyName, ref keyValuePairs);

            return keyValuePairs;
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
        private List<KeyValuePair<string, string>> GetKeyValuePairsFromJson(string parentPath, string json)
        {
            var keyValuePairs = new List<KeyValuePair<string, string>>();
            StringBuilder keyBuilder = new StringBuilder(parentPath);

            var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
            while (reader.Read())
            {
                string value = null;

                switch (reader.TokenType)
                {
                    case JsonTokenType.PropertyName:
                        keyBuilder.Append($":{reader.GetString()}");
                        break;
                    case JsonTokenType.Comment:
                        break;
                    case JsonTokenType.String:
                        value = reader.GetString();
                        break;
                    case JsonTokenType.Number:
                        if (reader.TryGetDouble(out double doubleValue))
                        {
                            value = doubleValue.ToString();
                        }

                        if (reader.TryGetInt32(out int intValue))
                        {
                            value = intValue.ToString();
                        }
                        break;
                    case JsonTokenType.True:
                        value = reader.GetBoolean().ToString();
                        break;
                    case JsonTokenType.False:
                        value = reader.GetBoolean().ToString();
                        break;
                    case JsonTokenType.None:
                    case JsonTokenType.StartObject:
                    case JsonTokenType.EndObject:
                    case JsonTokenType.StartArray:
                    case JsonTokenType.EndArray:
                    case JsonTokenType.Null:
                        break;
                    default:
                        break;
                }

                if (null != value)
                {
                    keyValuePairs.Add(new KeyValuePair<string, string>(keyBuilder.ToString(), value));
                    keyBuilder.Clear();
                    keyBuilder.Append(parentPath);
                }
            }

            return keyValuePairs;
        }

        /// <summary>
        /// Retrieve secret from AWS Secrets Manager
        /// </summary>
        /// <param name="key">AWS Secrets Manager key to retrieve.</param>
        /// <returns>A string representing the value in AWS Secrets Manager.</returns>
        private string GetAwsSecret(string key)
        {
            IAmazonSecretsManager awsSecretsManager = new AmazonSecretsManagerClient();

            GetSecretValueRequest request = new GetSecretValueRequest
            {
                SecretId = key
            };

            GetSecretValueResponse response = awsSecretsManager.GetSecretValueAsync(request).GetAwaiter().GetResult();

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
