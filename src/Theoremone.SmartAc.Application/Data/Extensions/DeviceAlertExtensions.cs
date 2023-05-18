using Theoremone.SmartAc.Application.DTOs;
using Theoremone.SmartAc.Application.Helpers;
using Theoremone.SmartAc.Application.Models;
using Theoremone.SmartAc.Domain.Entities;
using Theoremone.SmartAc.Domain.Models;

namespace Theoremone.SmartAc.Application.Data.Extensions
{
    public static class DeviceAlertExtensions
    {
        private static IEnumerable<Reading> DeviceReadings;
        public static List<DeviceAlertDto> ToDtosWithDeviceReadings(this PagedList<DeviceAlert> deviceAlerts, IEnumerable<Reading> deviceReadings)
        {
            DeviceReadings = deviceReadings;
            List<DeviceAlertDto> alerts = new();
            foreach (var deviceAlert in deviceAlerts)
            {
                var highestValue = GetHighestRecordedValue(deviceAlert.Type);
                var lowestValue = GetLowestRecordedValue(deviceAlert.Type);
               
                var alertDto = new DeviceAlertDto()
                {
                    DeviceSerialNumber = deviceAlert.DeviceSerialNumber,
                    DateCreated = deviceAlert.DateCreated,
                    DateLastRecorded = deviceAlert.DateLastRecorded,
                    DateRecorded = deviceAlert.DateRecorded,
                    DeviceAlertId = deviceAlert.DeviceAlertId,
                    Message = deviceAlert.Message,
                    State = deviceAlert.State,
                    Type = deviceAlert.Type,
                    Reading = deviceAlert.Data,
                    ReadingNonNumeric = deviceAlert.DataNonNumeric,
                    HighestReading = highestValue,
                    LowestReading = lowestValue
                };
                alerts.Add(alertDto);
            }
            return alerts;
        }

        private static decimal GetHighestRecordedValue(DeviceAlertType type)
        {
            if(DeviceReadings is null) return 0;
            return type switch
            {
                DeviceAlertType.OutOfRangeTemperature => DeviceReadings.Max(x => x.Temperature),
                DeviceAlertType.OutOfRangeCarbonMonoxide or DeviceAlertType.UnsafeCO => DeviceReadings.Max(x => x.CarbonMonoxide),
                DeviceAlertType.OutOfRangeHumidity => DeviceReadings.Max(x => x.Humidity),
                _ => 0
            };
        }
        private static decimal GetLowestRecordedValue(DeviceAlertType type)
        {
            if (DeviceReadings is null) return 0;
            return type switch
            {
                DeviceAlertType.OutOfRangeTemperature => DeviceReadings.Min(x => x.Temperature),
                DeviceAlertType.OutOfRangeCarbonMonoxide or DeviceAlertType.UnsafeCO => DeviceReadings.Min(x => x.CarbonMonoxide),
                DeviceAlertType.OutOfRangeHumidity => DeviceReadings.Min(x => x.Humidity),
                _ => 0
            };
        }
    }
}
