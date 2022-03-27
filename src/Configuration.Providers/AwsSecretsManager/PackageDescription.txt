This package is a custom configuration provider that can be used in startup to replace references in appsettings files to AWS Secrets Manager secrets with the secret values in the IConfigurationRoot.

**For example,**
```
     "Database:AwsSecret": "/dev/db/options"
```
**where the AWS Secrets Manager value is**
```
    {
        "timeoutMilliseconds": "5000",
        "maxPoolSize": "50"
    }
```
**can be turned into**
```
    "Database:timeoutMilliseconds" "5000",
    "Database:maxPoolSize": "50"
```