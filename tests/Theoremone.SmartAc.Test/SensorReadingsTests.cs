using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Theoremone.SmartAc.Application.Models;
using Theoremone.SmartAc.Controllers;
using Theoremone.SmartAc.Domain.Models;
using Theoremone.SmartAc.Infrastructure.DbContexts;
using Theoremone.SmartAc.Test.Infrastructure;

namespace Theoremone.SmartAc.Test;

public class SensorReadingsTests : IDisposable
{
    private readonly SmartAcApplication<DeviceIngestionController> _application;
    private readonly HttpClient _client;
    private readonly SmartAcContext _smartAcContext;

    public SensorReadingsTests()
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
        var deviceToken = await RegisterDeviceForToken(serialNumber, secret, firmware);
        var testData = GenerateDummyData();

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, deviceToken);

        var response = await _client
            .PostAsJsonAsync($"/api/v1/device/readings/batch", testData);

        var sensorData = await _smartAcContext.DeviceReadings
            .Where(reading => reading.DeviceSerialNumber == serialNumber)
            .OrderBy(s => s.RecordedDateTime)
            .ToListAsync();

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal(4, sensorData.Count);

        foreach (var (sentData, recordedData) in Enumerable.Zip(testData, sensorData))
        {
            Assert.Equal(sentData.RecordedDateTime, recordedData.RecordedDateTime);
            Assert.Equal(sentData.Temperature, recordedData.Temperature);
            Assert.Equal(sentData.Humidity, recordedData.Humidity);
            Assert.Equal(sentData.CarbonMonoxide, recordedData.CarbonMonoxide);
            Assert.Equal(sentData.Health, recordedData.Health);
        }
    }

    private async Task<string> RegisterDeviceForToken(string serialNumber, string secret, string firmware)
    {
        _client.DefaultRequestHeaders.Add("x-device-shared-secret", secret);

        var response = await _client
            .PostAsync($"/api/v1/device/{serialNumber}/registration?firmwareVersion={firmware}", default);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        return await response.Content.ReadAsStringAsync();
    }

    public static List<DeviceReadingRecord> GenerateDummyData()
    {
        return new List<DeviceReadingRecord>()
        {
            new(DateTimeOffset.UtcNow.AddMinutes(-40), 25.0m, 73.99m, 3.22m, DeviceHealth.Ok),
            new(DateTimeOffset.UtcNow.AddMinutes(-30), 25.19m, 73.98m, 3.22m, DeviceHealth.needs_service),
            new(DateTimeOffset.UtcNow.AddMinutes(-20), 25.2m, 73.99m, 3.21m, DeviceHealth.Ok),
            new(DateTimeOffset.UtcNow.AddMinutes(-10), 25.0m, 74.0m, 3.22m, DeviceHealth.needs_filter),
        };
    }
}