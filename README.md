# custom-configuration-providers

Custom Configuration Providers is a collection of configuration providers for .Net that extend Microsoft.Extensions.Configuration to pull configuration data from a variety of sources.  Details on how this custom configration provider is built can be found here:
[Implement a custom configuration provider in .NET](https://docs.microsoft.com/en-us/dotnet/core/extensions/custom-configuration-provider)

# AWS Secrets Manager Configuration Provider
## Overview
Using this custom configuration provider, you can put AWS Secrets Manager keys into your appsettings files and the values stored in AWS Secrets Manager will be loaded into the config at startup time as if they were in the appsettings file already.  What this auto-population of the secret configuration valus allow us to do is to leverage the [Options pattern in .Net](https://docs.microsoft.com/en-us/dotnet/core/extensions/options) along with Dependy Injection to automatically load these settings into the classes that need them.  In addition, having the settings pre-loaded at startup time means we can eliminate the need to mock an AwsSecretsManager and try to fake the settings at execution time.  We can simply use a test version of our appsettings file or in-memory configuration provider with the test values pre-populated and leave AWS Secrets Manager out of our unit tests completely.

So, here's the appsettings before and after running AddAwsSecretsManager:

<table>
  <thead>
    <tr>
      <th>
        appsettings.json file
      </th>
      <th>
        IConfiguration
      </th>
      <th>
        IConfiguration after .AddAwsSecretsManager()
      </th>
    </tr>
  </thead>
  <tbody>
    <tr>
      <td>
        <pre lang="json">
{
  "Database": {
    "AwsSecret": "/dev/db/options"
  }
}
        </pre>
      </td>
      <td>
"Database:AwsSecret": "/dev/db/options"<br>
      </td>
      <td>
"Database:server": "db1.myurl.com"<br>
"Database:port": "3306"<br>
"Database:timeout": "5"<br>
"Database:userName": "ApiReadOnly"<br>
"Database:password": "NunyaBizness!"<br>
      </td>
    </tr>
  </tbody>
</table>

## How it works
Let's say you have the following secret stored in AWS Secrets Manager under the key "/dev/db/options".
Setting  | Value
-------  | -----
server   | someserver.us-west-2.aws.amazon.com
port     | 3306
timeout  | 5
username | ApiReadOnly
password | NunyaBizness!

Using this provider you could put the following in your `appsettings.{environment}.json` file:
```json
{
  "Database": {
    "AwsSecret": "/dev/db/options"
  }
}
```
> **Note:**
> 
> In the next step, you'll see how you can choose any string you want in place of "AwsSecret"

Add the provider along with an AwsSecretsManager client (this is to allow for injecting mock versions or other versions of the client) and the name of the key you chose to put in your appsettings (e.g. "AwsSecret"):
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
> * In `AddAwsSecretsManager()` we're specifying the key to search and replace (e.g. "AwsSecret") with AWS Secrets Manager values.  You can choose whatever string you want.

When you build the config builder, the custom configuration provider will look in the appsettings files you've loaded for the key "AwsSecret" take it's value "/dev/db/options" and replace the whole key with the values retrieved from AWS Secrets Manager (in the IConfiguration tree in memory, not the appsettings file) like so:
<table>
  <thead>
    <tr>
      <th>
        appsettings.json file
      </th>
      <th>
        IConfiguration
      </th>
      <th>
        IConfiguration after .AddAwsSecretsManager()
      </th>
    </tr>
  </thead>
  <tbody>
    <tr>
      <td>
        <pre lang="json">
{
  "Database": {
    "AwsSecret": "/dev/db/options"
  }
}
        </pre>
      </td>
      <td>
"Database:AwsSecret": "/dev/db/options"<br>
      </td>
      <td>
"Database:server": "db1.myurl.com"<br>
"Database:port": "3306"<br>
"Database:timeout": "5"<br>
"Database:userName": "ApiReadOnly"<br>
"Database:password": "NunyaBizness!"<br>
      </td>
    </tr>
  </tbody>
</table>

## Options Pattern and Dependency Injection
With the AWS Secrets expanded using the configuration provider, we can use the settings in the Options Pattern to map the settings to a class, add it to the Dependency Injection IServiceCollection and inject it automatically into classes that need it.

### Create a class to hold the settings
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
> Since AWS Secrets Manager stores key-value pairs as <string,string> options classes shoud contain strings.  It is possible to add code to the Configuration Provider that parses the JSON into proper types, but it complicates the code that I think has more cost than benefit.

### Inject them into a Dependency Injection collection
```csharp
services.AddOptions<DatabaseOptions>()
    .Configure<IConfiguration>((settings, configuration) =>
    {
        configuration.GetSection("Database").Bind(settings);
    });
```
### And inject them into your classes
```csharp
private DatabaseOptions _databaseOptions;

public ServiceThatUsesDatabaseOptions(IOptions<DatabaseOptions> databaseOptions)
{
    _databaseOptions = databaseOptions.Value;
}
```

## Simplified Testing
Since we're pre-loading the real values in the ConfigurationBuilder at startup using the AWS Secrets Manager Configuration Provider, everything downstream is using the real secrets.  Therefore, when we're testing, we can just put our test values directly into our test config and eliminate AWS Secrets Manager from our unit tests.

### Example unit test
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
    _services.AddScoped<IServiceThatUsesDatabaseOptions, IServiceThatUsesDatabaseOptions>();

    _serviceProvider = _services.BuildServiceProvider();
    
    // Get the service to be tested
    var serviceToTest = _serviceProvider.GetRequiredService<IServiceThatUsesDatabaseOptions>();
}
