# Custom Configuration Providers

Custom Configuration Providers is a collection of configuration providers for .Net that extend Microsoft.Extensions.Configuration to pull configuration data from a variety of sources.  Details on how this custom configuration provider is built can be found here:
[Implement a custom configuration provider in .NET](https://docs.microsoft.com/en-us/dotnet/core/extensions/custom-configuration-provider)

## AWS Secrets Manager Configuration Provider

### Overview

Using this custom configuration provider, you can put AWS Secrets Manager keys into your appsettings files and the values stored in AWS Secrets Manager will be loaded into the config at startup time as if they were in the appsettings file already.  Using single AWS secrets keys we can selectively load just the secrets our app needs.

By resolving the secret values are startup allows us to do is to leverage the [Options pattern in .Net](https://docs.microsoft.com/en-us/dotnet/core/extensions/options) along with Dependency Injection to automatically load these settings into the classes that need them, meaning our code can be written in a way that it doesn't care where the settings come from.  This simplifies our testing because our unit tests can use an in-memory configuration provider with the test values pre-populated leaving them without the need to mock Secrets Manager or make connections from our CI/CD pipeline to a test instance of Secrets Manager.

So, here is an example of some values stored in the "/dev/db/options" key of our AWS Secrets Manager instance:

```json
{
  "server": "db1.myurl.com"
  "port": "3306"
  "timeout": "5"
  "userName": "ApiReadOnly"
  "password": "NunyaBizness!"
}
```

Here are our appsettings:

```json
  {
    "Database": {
      "AwsSecret": "/dev/db/options"
    }
  }
```

Here is what our IConfiguration values look like before and after we call .AddAwsSecretsManager, specifying "AwsSecret" as our placeholder key:

| IConfiguration before | IConfiguration after |
|---|---|
| "Database:AwsSecret": "/dev/db/options" | "Database:server": "db1.myurl.com" |
|| "Database:port": "3306" |
|| "Database:timeout": "5" |
|| "Database:userName": "ApiReadOnly" |
|| "Database:password": "NunyaBizness!" |

### How it works

Custom configuration providers work as extension methods for ConfigurationBuilder.  As long as the custom provider is included in your source and it's namespace is in your "using" statements, you should be able to just call ".AddAwsSecretsManager(...)".

In your app's startup code, add the provider to your configuration builder along with an AwsSecretsManager client (this is to allow for injecting mock versions or other versions of the client) and the name of the placeholder key you chose to put in your appsettings (e.g. "AwsSecret"):

```csharp
    var configurationBuilder = new ConfigurationBuilder();
    configurationBuilder.Sources.Clear();
    configurationBuilder
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile($"appsettings.{environment}.json", optional: true)
        .AddAwsSecretsManager(new AmazonSecretsManagerClient(Amazon.RegionEndpoint.USWest2), "AwsSecret")
        .AddEnvironmentVariables();
```

> A couple of things worth noting in this sample
>
> * We're supplying an AmazonSecretsManagerClient to simplify testing and to choose the region in our app startup.
> * In `AddAwsSecretsManager()` we're specifying the placeholder key to search and replace (e.g. "AwsSecret") with AWS Secrets Manager values.  You can choose whatever string you want.

When you call "Build" on the config builder, the custom configuration provider will look in the appsettings files you've loaded for the placeholder key you specified (e.g. "AwsSecret") take it's value "/dev/db/options" and replace the value with the values retrieved from AWS Secrets Manager (i.e. in the IConfiguration tree in memory, not the appsettings file) like so:

| IConfiguration before | IConfiguration after |
|---|---|
| "Database:AwsSecret": "/dev/db/options" | "Database:server": "db1.myurl.com" |
|| "Database:port": "3306" |
|| "Database:timeout": "5" |
|| "Database:userName": "ApiReadOnly" |
|| "Database:password": "NunyaBizness!" |

### Options Pattern and Dependency Injection

With the AWS Secrets expanded using the configuration provider, we can use the settings in the Options Pattern to map the settings to a class, add it to the Dependency Injection IServiceCollection and inject it automatically into classes that need it.

#### Create a class to hold the settings

```csharp
public class DatabaseOptions
{
  public string Server { get; set; }
  public string Port { get; set; }
  public string Timeout { get; set; }
  public string UserName { get; set; }
  public string Password { get; set; }
}
```

> Note:
>
> Since AWS Secrets Manager stores key-value pairs as <string,string> options classes should contain strings.  It is possible to add code to the Configuration Provider that parses the JSON into proper types, but it complicates the code in a way that I think has more cost than benefit.  Simpler to treat them as strings and convert them to the needed types by the classes that consume them.

#### Inject them into a Dependency Injection collection

```csharp
services.AddOptions<DatabaseOptions>()
    .Configure<IConfiguration>((settings, configuration) =>
    {
        configuration.GetSection("Database").Bind(settings);
    });
```

#### And inject them into your classes

```csharp
private DatabaseOptions _databaseOptions;

public ServiceThatUsesDatabaseOptions(IOptions<DatabaseOptions> databaseOptions)
{
    _databaseOptions = databaseOptions.Value;
}
```

### Simplified Testing

Since we're pre-loading the real values in the ConfigurationBuilder at startup using the AWS Secrets Manager Configuration Provider, everything downstream is using the real secrets.  Therefore, when we're testing, we can just put our test values directly into our test config and eliminate AWS Secrets Manager from our unit tests.

#### Example unit test

```csharp
public DependencyInjectionMockUnitTests()
{
    // setup test settings
    var appSettings = new Dictionary<string, string>
    {
        { "Database:server", "localhost" },
        { "Database:port", "3306" },
        { "Database:timeout", "1" },
        { "Database:username", "MyLocalAdmin" },
        { "Database:password", "StillNunyaBizness!" },
    };

    _configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(appSettings)
        .Build();

    // build the service collection
    _services = new ServiceCollection();

    _services.AddOptions<IOptions<DatabaseOptions>>()
        .Configure<IConfiguration>((settings, _configuration) =>
        {
            _configuration.GetSection("Database").Bind(settings);
        });
    _services.AddScoped<IServiceThatUsesDatabaseOptions, ServiceThatUsesDatabaseOptions>();

    _serviceProvider = _services.BuildServiceProvider();

    // Get the service to be tested
    var serviceToTest = _serviceProvider.GetRequiredService<IServiceThatUsesDatabaseOptions>();
}
