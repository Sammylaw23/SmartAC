using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Theoremone.SmartAc.Application.Data;
using Theoremone.SmartAc.Controllers;
using Theoremone.SmartAc.Infrastructure.DbContexts;
using Theoremone.SmartAc.Test.Infrastructure;

namespace Theoremone.SmartAc.Test;

public class RegisterDeviceTests : IDisposable
{
    private const int REPEAT_COUNT = 5;

    private readonly SmartAcApplication<DeviceIngestionController> _application;
    private readonly HttpClient _client;
    private readonly SmartAcContext _smartAcContext;

    public RegisterDeviceTests()
    {
        _application = new SmartAcApplication<DeviceIngestionController>();
        _client = _application.CreateClient();
        _smartAcContext = _application.Services.CreateScope().ServiceProvider
            .GetRequiredService<SmartAcContext>();
    }

    public void Dispose()
    {
        _client.Dispose();
        _application.Dispose();
    }

    [Theory]
    [InlineData("test-ABC-123-XYZ-001", "secret-ABC-123-XYZ-001", "1.0.0")]
    [InlineData("test-ABC-123-XYZ-002", "secret-ABC-123-XYZ-002", "1.0.0")]
    [InlineData("test-ABC-123-XYZ-003", "secret-ABC-123-XYZ-003", "1.0.0")]
    public async Task Succeeds_WithValidData(string serialNumber, string secret, string firmware)
    {
        await RegisterDeviceForToken(serialNumber, secret, firmware);
    }

    [Theory]
    [InlineData("test-ABC-123-XYZ-001", "secret-ABC-123-XYZ-001", "1.0.0")]
    [InlineData("test-ABC-123-XYZ-002", "secret-ABC-123-XYZ-002", "1.0.0")]
    [InlineData("test-ABC-123-XYZ-003", "secret-ABC-123-XYZ-003", "1.0.0")]
    public async Task DeactivatePreviousRegistrations(string serialNumber, string secret, string firmware)
    {
        foreach (var _ in Enumerable.Range(1, REPEAT_COUNT))
        {
            await RegisterDeviceForToken(serialNumber, secret, firmware);
            _client.DefaultRequestHeaders.Remove("x-device-shared-secret");
        }

        var registrations = _smartAcContext.DeviceRegistrations
            .Where(registration => registration.DeviceSerialNumber == serialNumber && registration.Active);

        Assert.Equal(1, await registrations.CountAsync());
    }

    [Theory]
    [InlineData("test-ABC-123-XYZ-001", "secret-ABC-123-XYZ-001", "1.0.0")]
    [InlineData("test-ABC-123-XYZ-002", "secret-ABC-123-XYZ-002", "1.0.0")]
    [InlineData("test-ABC-123-XYZ-003", "secret-ABC-123-XYZ-003", "1.0.0")]
    public async Task ProperlyUpdatesData(string serialNumber, string secret, string firmware)
    {
        string expectedFirmware = string.Empty;

        foreach (var value in Enumerable.Range(1, REPEAT_COUNT))
        {
            expectedFirmware = firmware[..^1] + value;
            await RegisterDeviceForToken(serialNumber, secret, expectedFirmware);
            _client.DefaultRequestHeaders.Remove("x-device-shared-secret");
        }

        var device = await _smartAcContext.Devices
            .FirstAsync(device => device.SerialNumber == serialNumber);

        var registrations = _smartAcContext.DeviceRegistrations
            .Where(registration => registration.DeviceSerialNumber == serialNumber);

        var orderedRegistrations = registrations.OrderBy(device => device.DeviceRegistrationId).ToList();
        var firstRegistration = orderedRegistrations.First();
        var lastRegistration = orderedRegistrations.Last();

        Assert.Equal(firstRegistration.RegistrationDate, device.FirstRegistrationDate);
        Assert.Equal(lastRegistration.RegistrationDate, device.LastRegistrationDate);
        Assert.Equal(expectedFirmware, device.FirmwareVersion);
    }

    [Theory]
    [InlineData("test-ABC-123-XYZ-001", "secret-ABC-123-XYZ-001", "1.0.0")]
    [InlineData("test-ABC-123-XYZ-002", "secret-ABC-123-XYZ-002", "1.10.3")]
    [InlineData("test-ABC-123-XYZ-003", "secret-ABC-123-XYZ-003", "0.1.0")]
    [InlineData("test-ABC-123-XYZ-003", "secret-ABC-123-XYZ-003", "0.1.0-BETA")]
    public async Task SuccessWhenValidFirmware(string serialNumber, string secret, string firmware)
    {
        // spec for feature BE-DEV-1 shows the details for firmware version validation.  

        _client.DefaultRequestHeaders.Add("x-device-shared-secret", secret);
        var response = await _client
            .PostAsync($"/api/v1/device/{serialNumber}/registration?firmwareVersion={firmware}", default);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("test-ABC-123-XYZ-001", "secret-ABC-123-XYZ-001", "1.0.0.1")]
    [InlineData("test-ABC-123-XYZ-002", "secret-ABC-123-XYZ-002", "something")]
    [InlineData("test-ABC-123-XYZ-003", "secret-ABC-123-XYZ-003", "1.0")]
    [InlineData("test-ABC-123-XYZ-003", "secret-ABC-123-XYZ-003", "a.b.c")]
    [InlineData("test-ABC-123-XYZ-003", "secret-ABC-123-XYZ-003", "..")]
    public async Task ReturnErrorWhenInvalidFirmware(string serialNumber, string secret, string firmware)
    {
        // spec for feature BE-DEV-1 shows the details for firmware version validation.  

        _client.DefaultRequestHeaders.Add("x-device-shared-secret", secret);
        var response = await _client
            .PostAsync($"/api/v1/device/{serialNumber}/registration?firmwareVersion={firmware}", default);

        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        var expectedError = "The firmware value does not match semantic versioning format.";

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(expectedError, problemDetails?.Errors["firmwareVersion"].FirstOrDefault());
    }

    private async Task<string> RegisterDeviceForToken(string serialNumber, string secret, string firmware)
    {
        _client.DefaultRequestHeaders.Add("x-device-shared-secret", secret);

        var response = await _client
            .PostAsync($"/api/v1/device/{serialNumber}/registration?firmwareVersion={firmware}", default);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        return await response.Content.ReadAsStringAsync();
    }
}