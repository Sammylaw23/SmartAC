using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Theoremone.SmartAc.Controllers;
using Theoremone.SmartAc.Test.Infrastructure;

namespace Theoremone.SmartAc.Test;

public class DeviceAuthenticationTests : IDisposable
{
    private readonly SmartAcApplication<DeviceIngestionController> _application;
    private readonly HttpClient _client;

    public DeviceAuthenticationTests()
    {
        _application = new SmartAcApplication<DeviceIngestionController>();
        _client = _application.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _application.Dispose();
    }

    [Theory]
    [InlineData("test-ABC-123-XYZ-001", "secret-ABC-123-XYZ-001-invalid", "1.0.0")]
    [InlineData("test-ABC-123-XYZ-002", "secret-ABC-123-XYZ-002-invalid", "1.0.0")]
    [InlineData("test-ABC-123-XYZ-003", "secret-ABC-123-XYZ-003-invalid", "1.0.0")]
    public async Task Unauthorized_IfInvalidDataShareSecret(string serialNumber, string secret, string firmware)
    {
        _client.DefaultRequestHeaders.Add("x-device-shared-secret", secret);

        var response = await _client
            .PostAsync($"/api/v1/device/{serialNumber}/registration?firmwareVersion={firmware}", default);

        var expecteErrorMessage = "Something is wrong on the information provided, please review.";
        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(expecteErrorMessage, problemDetails?.Detail);
        Assert.Equal(StatusCodes.Status401Unauthorized, problemDetails?.Status);
    }

    [Theory]
    [InlineData("test-ABC-123-XYZ-001", "secret-ABC-123-XYZ-001", "1.0.0")]
    [InlineData("test-ABC-123-XYZ-002", "secret-ABC-123-XYZ-002", "1.0.0")]
    [InlineData("test-ABC-123-XYZ-003", "secret-ABC-123-XYZ-003", "1.0.0")]
    public async Task Forbidden_IfDeactivatedTokenIsUsed(string serialNumber, string secret, string firmware)
    {
        var oldDeviceToken = await RegisterDeviceForToken(serialNumber, secret, firmware);
        var _ = await RegisterDeviceForToken(serialNumber, secret, firmware);

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, oldDeviceToken);

        var response = await _client
            .PostAsync($"/api/v1/device/readings/batch", default);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<string> RegisterDeviceForToken(string serialNumber, string secret, string firmware)
    {
        _client.DefaultRequestHeaders.Add("x-device-shared-secret", secret);

        var response = await _client
            .PostAsync($"/api/v1/device/{serialNumber}/registration?firmwareVersion={firmware}", default);

        _client.DefaultRequestHeaders.Remove("x-device-shared-secret");

        return await response.Content.ReadAsStringAsync();
    }
}