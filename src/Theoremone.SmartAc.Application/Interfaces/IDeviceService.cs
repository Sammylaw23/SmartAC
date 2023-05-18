using Theoremone.SmartAc.Application.DTOs;
using Theoremone.SmartAc.Application.Models;
using Theoremone.SmartAc.Domain.Entities;

namespace Theoremone.SmartAc.Application.Interfaces
{
    public interface IDeviceService
    {
        Task AddDeviceSensorReadings(string serialNumber, IEnumerable<DeviceReadingRecord> sensorReadings);
        Task<string> RegisterDeviceForAuth(RegisterDeviceDto deviceDto);
        Task<List<DeviceReadingRecordWithErrors>> RetrieveValidReadingsAndThrowIfInvalid(IEnumerable<DeviceReadingRecord> sensorReadings, string serialNumber);
        Task<(IEnumerable<DeviceAlertDto>, string)> GetDeviceAlertReadingsAsync(string serialNumber, DeviceParameters deviceParameters);
    }
}