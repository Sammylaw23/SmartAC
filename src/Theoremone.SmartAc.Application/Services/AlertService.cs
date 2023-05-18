using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Theoremone.SmartAc.Application.Interfaces;
using Theoremone.SmartAc.Application.Models;
using Theoremone.SmartAc.Application.Settings;
using Theoremone.SmartAc.Domain.Entities;
using Theoremone.SmartAc.Domain.Models;
using Theoremone.SmartAc.Infrastructure.DbContexts;
namespace Theoremone.SmartAc.Application.Services
{
    public class AlertService : IAlertService
    {
        private readonly SmartAcContext _smartAcContext;
        private readonly ILogger<AlertService> _logger;
        private readonly SensorLimitsOptions _sensorLimitConfig;
        private readonly GeneralSettingsOptions _generalSettingsConfig;

        public AlertService(
            IOptionsMonitor<SensorLimitsOptions> sensorLimitConfig,
            IOptionsMonitor<GeneralSettingsOptions> generalSettingsConfig,
        SmartAcContext smartAcContext,
        ILogger<AlertService> logger
            )
        {
            _sensorLimitConfig = sensorLimitConfig.CurrentValue;
            _generalSettingsConfig = generalSettingsConfig.CurrentValue;
            _smartAcContext = smartAcContext;
            _logger = logger;
        }
        public async Task ProduceAlerts(string serialNumber, IEnumerable<DeviceReadingRecordWithErrors> readingsWithError)
        {
            List<DeviceAlert> deviceAlertsFromReadings = await GenerateDeviceAlertsForDeviceReadings(readingsWithError);

            //INFO: If there are multiple occurrences of the same type of alert, the date recorded by the device will be different. In such cases, keep the most recent.
            var distinctAlerts = deviceAlertsFromReadings.OrderByDescending(x => x.DateRecorded).GroupBy(x => x.Type).Select(y => y.FirstOrDefault()).ToList();

            List<DeviceAlert> deviceAlertsFromDb = await GetAlertsBySerialNumber_OrderedDescendingAndOfDistinctAlertType(serialNumber);

            if (!deviceAlertsFromDb.Any() && distinctAlerts.Any())
            {
                await _smartAcContext.DeviceAlerts.AddRangeAsync(distinctAlerts);
                await _smartAcContext.SaveChangesAsync();
                _logger.LogInformation("Successfully saved alerts for device with serial number: {0} at {1}", serialNumber, DateTime.Now);
            }

            //INFO: Another call is made to the database to fetch the device alerts inserted above
            if (!deviceAlertsFromDb.Any())
                deviceAlertsFromDb = await GetAlertsBySerialNumber_OrderedDescendingAndOfDistinctAlertType(serialNumber);


            var (alertFromDeviceReading_Temperature, alertFromDb_Temperature)
                = GetAlertsFromReadingAndDB(deviceAlertsFromReadings, deviceAlertsFromDb, DeviceAlertType.OutOfRangeTemperature);
            var (alertFromDeviceReading_Humidity, alertFromDb_Humidity)
                = GetAlertsFromReadingAndDB(deviceAlertsFromReadings, deviceAlertsFromDb, DeviceAlertType.OutOfRangeHumidity);
            var (alertFromDeviceReading_CarbonMonoxide, alertFromDb_CarbonMonoxide)
                = GetAlertsFromReadingAndDB(deviceAlertsFromReadings, deviceAlertsFromDb, DeviceAlertType.OutOfRangeCarbonMonoxide);
            var (alertFromDeviceReading_UnsafeCo, alertFromDb_UnsafeCo)
                = GetAlertsFromReadingAndDB(deviceAlertsFromReadings, deviceAlertsFromDb, DeviceAlertType.UnsafeCO);
            var (alertFromDeviceReading_Health, alertFromDb_Health)
                = GetAlertsFromReadingAndDB(deviceAlertsFromReadings, deviceAlertsFromDb, DeviceAlertType.PoorHealth);

            var orderedReadings = readingsWithError.OrderBy(x => x.RecordedDateTime);

            foreach (var reading in orderedReadings)
            {
                if (alertFromDb_Temperature != null)
                    MergeTemperatureAlerts(reading, alertFromDb_Temperature, alertFromDeviceReading_Temperature);
                if (alertFromDb_Humidity != null)
                    MergeHumidityAlerts(reading, alertFromDb_Humidity, alertFromDeviceReading_Humidity);
                if (alertFromDb_CarbonMonoxide != null)
                    MergeCarbonMoxideAlerts(reading, alertFromDb_CarbonMonoxide, alertFromDeviceReading_CarbonMonoxide);
                if (alertFromDb_UnsafeCo != null)
                    MergeUnsafeCOAlerts(reading, alertFromDb_UnsafeCo, alertFromDeviceReading_UnsafeCo);
                if (alertFromDb_Health != null)
                    MergePoorHealthAlerts(reading, alertFromDb_Health, alertFromDeviceReading_Health);
            }
            await _smartAcContext.SaveChangesAsync();
        }

        #region Private Methods
        private async Task<List<DeviceAlert>> GenerateDeviceAlertsForDeviceReadings(IEnumerable<DeviceReadingRecordWithErrors> readings)
        {
            List<DeviceAlert> deviceAlerts = new();
            foreach (var reading in readings)
            {
                await foreach (var alert in CreateAlertsFromDeviceReadingErrors(reading))
                    deviceAlerts.Add(alert);
            }
            return deviceAlerts;
        }
        private (DeviceAlert?, DeviceAlert?) GetAlertsFromReadingAndDB(List<DeviceAlert> alertsFromReadings, List<DeviceAlert> alertsFromDb, DeviceAlertType type)
        {
            return (
                alertsFromReadings.FirstOrDefault(x => x.Type == type),
                alertsFromDb.FirstOrDefault(x => x.Type == type)
                );

        }

        //TODO: REFACTOR the following methods. They have the similar logic
        private void MergeTemperatureAlerts(DeviceReadingRecordWithErrors reading, DeviceAlert alertFromDb, DeviceAlert alertFromDeviceReading)
        {
            var newAlertTooOlder = NewDateIsGreaterThanBySpecifiedInterval(reading.RecordedDateTime, alertFromDb.DateRecorded);

            if (reading.Temperature < _sensorLimitConfig.LowerTemperature ||
                reading.Temperature > _sensorLimitConfig.HigherTemperature)
            {
                if (alertFromDb is not null && !newAlertTooOlder)
                {
                    alertFromDb.SetState(ResolutionState.New, reading.RecordedDateTime);
                    _smartAcContext.Update(alertFromDb);
                }
                else
                {
                    _smartAcContext.DeviceAlerts.Add(alertFromDeviceReading);
                }
            }
            else
            {
                if (!newAlertTooOlder)
                {
                    alertFromDb.SetState(ResolutionState.Resolved, reading.RecordedDateTime);
                    alertFromDb.Data = reading.Temperature;
                    _smartAcContext.Update(alertFromDb);
                }
            }
        }
        private void MergeHumidityAlerts(DeviceReadingRecordWithErrors reading, DeviceAlert alertFromDb, DeviceAlert alertFromDeviceReading)
        {
            var newAlertTooOlder = NewDateIsGreaterThanBySpecifiedInterval(reading.RecordedDateTime, alertFromDb.DateRecorded);
            if (reading.Humidity < _sensorLimitConfig.LowerHumidity ||
                reading.Humidity > _sensorLimitConfig.HigherHumidity)
            {
                if (alertFromDb is not null && !newAlertTooOlder)
                {
                    alertFromDb.SetState(ResolutionState.New, reading.RecordedDateTime);
                    _smartAcContext.Update(alertFromDb);
                }
                else
                {
                    _smartAcContext.DeviceAlerts.Add(alertFromDeviceReading);
                }
            }
            else
            {
                if (!newAlertTooOlder)
                {
                    alertFromDb.SetState(ResolutionState.Resolved, reading.RecordedDateTime);
                    alertFromDb.Data = reading.Humidity;
                    _smartAcContext.Update(alertFromDb);
                }
            }
        }
        private void MergeCarbonMoxideAlerts(DeviceReadingRecordWithErrors reading, DeviceAlert alertFromDb, DeviceAlert alertFromDeviceReading)
        {
            var newAlertTooOlder = NewDateIsGreaterThanBySpecifiedInterval(reading.RecordedDateTime, alertFromDb.DateRecorded);
            if (reading.CarbonMonoxide < _sensorLimitConfig.LowerCarbonMonoxide ||
                reading.CarbonMonoxide > _sensorLimitConfig.HigherCarbonMonoxide)
            {
                if (alertFromDb is not null && !newAlertTooOlder)
                {
                    alertFromDb.SetState(ResolutionState.New, reading.RecordedDateTime);
                    _smartAcContext.Update(alertFromDb);
                }
                else
                {
                    _smartAcContext.DeviceAlerts.Add(alertFromDeviceReading);
                }
            }
            else
            {
                if (!newAlertTooOlder)
                {
                    alertFromDb.SetState(ResolutionState.Resolved, reading.RecordedDateTime);
                    alertFromDb.Data = reading.CarbonMonoxide;
                    _smartAcContext.Update(alertFromDb);
                }
            }
        }
        private void MergeUnsafeCOAlerts(DeviceReadingRecordWithErrors reading, DeviceAlert alertFromDb, DeviceAlert alertFromDeviceReading)
        {
            var newAlertTooOlder = NewDateIsGreaterThanBySpecifiedInterval(reading.RecordedDateTime, alertFromDb.DateRecorded);
            if (reading.CarbonMonoxide > _sensorLimitConfig.UnsafeCOThreshold)
            {
                if (alertFromDb is not null && !newAlertTooOlder)
                {
                    alertFromDb.SetState(ResolutionState.New, reading.RecordedDateTime);
                    _smartAcContext.Update(alertFromDb);
                }
                else
                {
                    _smartAcContext.DeviceAlerts.Add(alertFromDeviceReading);
                }
            }
            else
            {
                if (!newAlertTooOlder)
                {
                    alertFromDb.SetState(ResolutionState.Resolved, reading.RecordedDateTime);
                    alertFromDb.Data = reading.CarbonMonoxide;
                    _smartAcContext.Update(alertFromDb);
                }
            }
        }
        private void MergePoorHealthAlerts(DeviceReadingRecordWithErrors reading, DeviceAlert alertFromDb, DeviceAlert alertFromDeviceReading)
        {
            var newAlertTooOlder = NewDateIsGreaterThanBySpecifiedInterval(reading.RecordedDateTime, alertFromDb.DateRecorded);
            if (reading.Health != DeviceHealth.Ok)
            {
                if (alertFromDb is not null && !newAlertTooOlder)
                {
                    alertFromDb.SetState(ResolutionState.New, reading.RecordedDateTime);
                    _smartAcContext.Update(alertFromDb);
                }
                else
                {
                    _smartAcContext.DeviceAlerts.Add(alertFromDeviceReading);
                }
            }
            else
            {
                if (!newAlertTooOlder)
                {
                    alertFromDb.SetState(ResolutionState.Resolved, reading.RecordedDateTime);
                    alertFromDb.DataNonNumeric = reading.Health.ToString();
                    _smartAcContext.Update(alertFromDb);
                }
            }
        }
        private async Task<List<DeviceAlert>> GetAlertsBySerialNumber_OrderedDescendingAndOfDistinctAlertType(string serialNumber)
        {
            var recentAlerts = await _smartAcContext.DeviceAlerts
                 .Where(x => x.DeviceSerialNumber == serialNumber)
                 .Take(30).OrderByDescending(x => x.DateRecorded)
                 .GroupBy(x => x.Type)
                 .Select(y => y.FirstOrDefault())
                 .ToListAsync();
            return recentAlerts!;
        }

        /// <summary>
        /// This returns true if newDate is greater than oldDate by number of minutes specified by interval variable.
        /// </summary>
        /// <param name="newDate">New date</param>
        /// <param name="oldDate"></param>
        /// <param name="interval"></param>
        /// <returns></returns>
        private bool NewDateIsGreaterThanBySpecifiedInterval(DateTimeOffset newDate, DateTimeOffset oldDate)
        {
            if (newDate < oldDate)
                return false;

            return (newDate.UtcDateTime - oldDate.UtcDateTime).TotalMinutes > _generalSettingsConfig.TimeIntervalToMergeAlerts;
        }
        private async Task<DeviceAlert> CreateAlert(DeviceReadingRecordWithErrors reading, DateTime dateCreated)
        {
            var alert = new DeviceAlert()
            {
                DateCreated = dateCreated,
                DateRecorded = reading.RecordedDateTime,
                State = ResolutionState.New,
                DeviceSerialNumber = reading.DeviceSerialNumber


            };
            await Task.Delay(1);
            return alert;
        }
        private async IAsyncEnumerable<DeviceAlert> CreateAlertsFromDeviceReadingErrors(DeviceReadingRecordWithErrors reading)
        {
            var dateNow = DateTime.UtcNow;

            if (reading.Error.Temperature is not null)
            {
                var alertTemperature = await CreateAlert(reading, dateNow);
                alertTemperature.Message = reading.Error.Temperature;
                alertTemperature.Type = DeviceAlertType.OutOfRangeTemperature;
                alertTemperature.Data = reading.Temperature;
                yield return alertTemperature;
            }
            if (reading.Error.Humidity is not null)
            {
                var alertHumidity = await CreateAlert(reading, dateNow);
                alertHumidity.Message = reading.Error.Humidity;
                alertHumidity.Type = DeviceAlertType.OutOfRangeHumidity;
                alertHumidity.Data = reading.Humidity;
                yield return alertHumidity;
            }
            if (reading.Error.CarbonMonoxide is not null)
            {
                var alertCarbonMonoxide = await CreateAlert(reading, dateNow);
                alertCarbonMonoxide.Message = reading.Error.CarbonMonoxide;
                alertCarbonMonoxide.Type = DeviceAlertType.OutOfRangeCarbonMonoxide;
                alertCarbonMonoxide.Data = reading.CarbonMonoxide;
                yield return alertCarbonMonoxide;
            }
            if (reading.Error.UnsafeCO is not null)
            {
                var alertUnsafeCO = await CreateAlert(reading, dateNow);
                alertUnsafeCO.Message = reading.Error.UnsafeCO;
                alertUnsafeCO.Type = DeviceAlertType.UnsafeCO;
                alertUnsafeCO.Data = reading.CarbonMonoxide;
                yield return alertUnsafeCO;
            }
            if (reading.Error.Unhealthy is not null)
            {
                var alertHealth = await CreateAlert(reading, dateNow);
                alertHealth.Message = reading.Error.Unhealthy;
                alertHealth.Type = DeviceAlertType.PoorHealth;
                alertHealth.DataNonNumeric = reading.Health.ToString();
                yield return alertHealth;
            }

        }
        #endregion
    }
}