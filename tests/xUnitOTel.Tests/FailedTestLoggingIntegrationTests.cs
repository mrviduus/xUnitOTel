using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace xUnitOTel.Tests;

public class FailedTestLoggingIntegrationTests
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FailedTestLoggingIntegrationTests> _logger;

    public FailedTestLoggingIntegrationTests(TestSetup setup)
    {
        var factory = setup.Host.Services.GetRequiredService<IHttpClientFactory>();
        _httpClient = factory.CreateClient();
        _logger = setup.Host.Services.GetRequiredService<ILogger<FailedTestLoggingIntegrationTests>>();
    }

    [Fact]
    public async Task Test1_FetchGoogle_AndFail()
    {
        _logger.LogInformation("Test1: Starting HTTP request to Google");

        var response = await _httpClient.GetAsync("https://www.google.com");
        _logger.LogInformation("Test1: Got response {StatusCode}", response.StatusCode);

        _logger.LogWarning("Test1: About to fail intentionally");
        Assert.Fail("Test1: Intentional failure after Google request");
    }

    [Fact]
    public async Task Test2_FetchGitHub_AndFail()
    {
        _logger.LogInformation("Test2: Starting HTTP request to GitHub");

        var response = await _httpClient.GetAsync("https://api.github.com");
        _logger.LogInformation("Test2: Got response {StatusCode}", response.StatusCode);

        _logger.LogError("Test2: Simulating an error condition");
        Assert.Fail("Test2: Intentional failure after GitHub request");
    }

    [Fact]
    public async Task Test3_FetchMultipleUrls_AndFail()
    {
        _logger.LogInformation("Test3: Starting multiple HTTP requests");

        var google = await _httpClient.GetAsync("https://www.google.com");
        _logger.LogInformation("Test3: Google responded with {StatusCode}", google.StatusCode);

        var github = await _httpClient.GetAsync("https://api.github.com");
        _logger.LogInformation("Test3: GitHub responded with {StatusCode}", github.StatusCode);

        _logger.LogCritical("Test3: Critical failure incoming!");
        Assert.Fail("Test3: Intentional failure after multiple requests");
    }

    [Fact]
    public async Task Test4_FetchWithDelay_AndFail()
    {
        _logger.LogInformation("Test4: Starting with delay");
        await Task.Delay(100);

        _logger.LogDebug("Test4: Delay completed, fetching httpbin");
        var response = await _httpClient.GetAsync("https://httpbin.org/get");
        _logger.LogInformation("Test4: httpbin responded with {StatusCode}", response.StatusCode);

        _logger.LogWarning("Test4: This test will fail now");
        Assert.Fail("Test4: Intentional failure after delayed request");
    }

    [Fact]
    public async Task Test5_FetchAndLogException_AndFail()
    {
        _logger.LogInformation("Test5: Starting request");

        try
        {
            var response = await _httpClient.GetAsync("https://www.google.com");
            _logger.LogInformation("Test5: Got {StatusCode}", response.StatusCode);

            throw new InvalidOperationException("Test5: Simulated exception");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Test5: Caught simulated exception");
        }

        Assert.Fail("Test5: Intentional failure with exception logging");
    }
}
