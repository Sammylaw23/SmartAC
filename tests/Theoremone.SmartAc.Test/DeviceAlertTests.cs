using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Theoremone.SmartAc.Application.Models;
using Theoremone.SmartAc.Controllers;
using Theoremone.SmartAc.Domain.Models;
using Theoremone.SmartAc.Infrastructure.DbContexts;
using Theoremone.SmartAc.Test.Infrastructure;

namespace Thereomone.SmartAc.Test
{
    public class DeviceAlertTests
    {
        private readonly SmartAcApplication<DeviceIngestionController> _application;
        private readonly HttpClient _client;
        private readonly SmartAcContext _smartAcContext;

        public DeviceAlertTests()
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

        [Fact]
        public async Task ProduceAlertsForCorrectReadings()
        {
            var token = await GetBearerTokenForTest();
            var data = GenerateSensorReadingDummyData();
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, token);

            var response = await _client
                .PostAsJsonAsync($"/api/v1/device/readings/batch", data);

            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

            var temperatureAlert = _smartAcContext.DeviceAlerts.Where(x => x.Type == DeviceAlertType.OutOfRangeTemperature).FirstOrDefault();
            var healthAlert = _smartAcContext.DeviceAlerts.Where(x => x.Type == DeviceAlertType.PoorHealth).FirstOrDefault();
            Assert.NotNull(temperatureAlert);
            Assert.Equal(ResolutionState.New, temperatureAlert?.State);
            Assert.Equal("Device is reporting health problem: needs_service", healthAlert?.Message);
        }

        private async Task<string> GetBearerTokenForTest()
        {
            string serialNumber = "test-ABC-123-XYZ-001";
            string secret = "secret-ABC-123-XYZ-001";
            string firmware = "1.0.0";

            _client.DefaultRequestHeaders.Add("x-device-shared-secret", secret);

            var response = await _client
                .PostAsync($"/api/v1/device/{serialNumber}/registration?firmwareVersion={firmware}", default);

            _client.DefaultRequestHeaders.Remove("x-device-shared-secret");

            return await response.Content.ReadAsStringAsync();
        }

        public static List<DeviceReadingRecord> GenerateSensorReadingDummyData()
        {
            return new List<DeviceReadingRecord>()
            {
                new(DateTimeOffset.UtcNow.AddMinutes(-40), 25.00m, 101.98m, 103.22m, DeviceHealth.needs_filter),
                new(DateTimeOffset.UtcNow.AddMinutes(-30), 150.0m, 105.0m, 10.22m, DeviceHealth.needs_service),
            };
        }
    }
}
