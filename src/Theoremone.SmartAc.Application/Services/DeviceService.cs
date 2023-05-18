using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Text.Json;
using System.Text.RegularExpressions;
using Theoremone.SmartAc.Application.Constants;
using Theoremone.SmartAc.Application.Data.Extensions;
using Theoremone.SmartAc.Application.DTOs;
using Theoremone.SmartAc.Application.Exceptions;
using Theoremone.SmartAc.Application.Helpers;
using Theoremone.SmartAc.Application.Interfaces;
using Theoremone.SmartAc.Application.Interfaces.Identity;
using Theoremone.SmartAc.Application.Models;
using Theoremone.SmartAc.Domain.Entities;
using Theoremone.SmartAc.Domain.Models;
using Theoremone.SmartAc.Infrastructure.DbContexts;

namespace Theoremone.SmartAc.Application.Services;
public class DeviceService : IDeviceService
{
    private readonly SmartAcContext _smartAcContext;
    private readonly ILogger<DeviceService> _logger;
    private readonly ISmartAcJwtService _smartAcJwtService;
    private IValidator<DeviceReadingRecord> _validator;

    public DeviceService(
    SmartAcContext smartAcContext,
    ISmartAcJwtService smartAcJwtService,
    ILogger<DeviceService> logger,
    IValidator<DeviceReadingRecord> validator
        )
    {
        _smartAcContext = smartAcContext;
        _logger = logger;
        _smartAcJwtService = smartAcJwtService;
        _validator = validator;
    }

    public async Task AddDeviceSensorReadings(string serialNumber, IEnumerable<DeviceReadingRecord> sensorReadings)
    {
        var receivedDate = DateTime.UtcNow;
        var deviceReadings = sensorReadings.Select(reading => reading.ToDeviceReading(serialNumber, receivedDate))
            .ToList();
        await _smartAcContext.DeviceReadings.AddRangeAsync(deviceReadings);
        await _smartAcContext.SaveChangesAsync();
    }
    public async Task<string> RegisterDeviceForAuth(RegisterDeviceDto deviceDto)
    {
        if (!FirmwareVersionIsValid(deviceDto.firmwareVersion))
            throw new ValidationProblemException("firmwareVersion", "The firmware value does not match semantic versioning format.");

        var device = await GetDeviceOrThrowIfNotExist(deviceDto.serialNumber, deviceDto.sharedSecret);

        var (tokenId, jwtToken) = _smartAcJwtService.GenerateJwtFor(deviceDto.serialNumber, SmartJwtServiceConstants.JwtScopeDeviceIngestionService);
        if (string.IsNullOrEmpty(jwtToken))
        {
            _logger.LogError("Unable to generate JWT token. for serial number:{0}", deviceDto.serialNumber);
            throw new ProblemException("Something is wrong on the information provided, please review.");
        }

        await ActivateDeviceRegistration(device, deviceDto.firmwareVersion, tokenId);

        return jwtToken;
    }
    public async Task<List<DeviceReadingRecordWithErrors>> RetrieveValidReadingsAndThrowIfInvalid(IEnumerable<DeviceReadingRecord> sensorReadings, string serialNumber)
    {
        List<DeviceReadingRecordWithErrors> deviceReadingRecordWithErrorsList = new();

        foreach (var sensorReading in sensorReadings)
        {
            var result = await _validator.ValidateAsync(sensorReading);
            if (result.Errors.Count <= 0)
                continue;
            var deviceReadingRecordWithErrors = LoadErrorDetail(serialNumber, sensorReading, result.Errors);
            deviceReadingRecordWithErrorsList.Add(deviceReadingRecordWithErrors);
        }

        return deviceReadingRecordWithErrorsList;
    }
    private static DeviceReadingRecordWithErrors LoadErrorDetail(string serialNumber, DeviceReadingRecord sensorReading, IEnumerable<ValidationFailure> errors)
    {
        DeviceReadingRecordWithErrors deviceReadingRecordWithErrors = new()
        {
            Temperature = sensorReading.Temperature,
            CarbonMonoxide = sensorReading.CarbonMonoxide,
            Humidity = sensorReading.Humidity,
            Health = sensorReading.Health,
            RecordedDateTime = sensorReading.RecordedDateTime,
            DeviceSerialNumber = serialNumber
        };

        foreach (var error in errors)
        {
            //TODO: use constants instead of magic strings
            _ = error.PropertyName switch
            {
                "Temperature" => deviceReadingRecordWithErrors.Error.Temperature = error.ErrorMessage,
                "CarbonMonoxide" => (error.ErrorCode == "LessThanOrEqualValidator") ?
               deviceReadingRecordWithErrors.Error.UnsafeCO = error.ErrorMessage :
               deviceReadingRecordWithErrors.Error.CarbonMonoxide = error.ErrorMessage,
                "Humidity" => deviceReadingRecordWithErrors.Error.Humidity = error.ErrorMessage,
                "Health" => deviceReadingRecordWithErrors.Error.Unhealthy = error.ErrorMessage,
                _ => ""
            };
        }

        return deviceReadingRecordWithErrors;
    }
    public async Task<(IEnumerable<DeviceAlertDto>, string)> GetDeviceAlertReadingsAsync(string serialNumber, DeviceParameters deviceParameters)
    {
        var query = _smartAcContext.DeviceAlerts.AsQueryable().Where(x => x.DeviceSerialNumber.Equals(serialNumber));
        if (deviceParameters.State != ResolutionState.All)
            query = query.Where(x => x.State == deviceParameters.State);
        query = query.OrderByDescending(x => x.DateRecorded);

        var pagedDbAlerts = await PagedList<DeviceAlert>.ToPagedListAsync(query, deviceParameters.PageNumber, deviceParameters.PageSize);

        DateTimeOffset dateRecorded = pagedDbAlerts.OrderByDescending(x => x.DateRecorded)
            .Where(x => x.State == ResolutionState.Resolved)
            .Skip(1)
            .Select(x => x.DateRecorded)
            .FirstOrDefault();

        var readings = GetDeviceReadingsBySerialNumber(serialNumber, dateRecorded);

        List<DeviceAlertDto> alerts = pagedDbAlerts.ToDtosWithDeviceReadings(readings);

        var metadata = new
        {
            pagedDbAlerts.TotalCount,
            pagedDbAlerts.PageSize,
            pagedDbAlerts.CurrentPage,
            pagedDbAlerts.TotalPages,
            pagedDbAlerts.HasNext,
            pagedDbAlerts.HasPrevious
        };
        _logger.LogInformation($"Returned {pagedDbAlerts.TotalCount} device alerts from database.");

        return (alerts, JsonSerializer.Serialize(metadata));
    }
    private List<Reading> GetDeviceReadingsBySerialNumber(string serialNumber, DateTimeOffset dateRecorded)
    {
        var query = _smartAcContext.DeviceReadings.Where(x => x.DeviceSerialNumber == serialNumber);
        var dbReadings = query.Where(x => x.RecordedDateTime > dateRecorded).ToList();

        var readings = (from d in dbReadings
                        select new Reading
                        {
                            Temperature = d.Temperature,
                            CarbonMonoxide = d.CarbonMonoxide,
                            Humidity = d.Humidity
                        }).ToList();
        return readings;
    }



    #region Private Methods
    private async Task<Device> GetDeviceOrThrowIfNotExist(string serialNumber, string sharedSecret)
    {
        var device = await GetDevice(serialNumber, sharedSecret);

        if (string.IsNullOrEmpty(device.SerialNumber))
        {
            _logger.LogDebug("There is not a matching device for serial number {serialNumber} and the secret provided.", serialNumber);
            throw new ProblemException("Something is wrong on the information provided, please review.");
        }
        return device;
    }
    private bool FirmwareVersionIsValid(string firmwareVersion)
    {
        var regex = @"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-((?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+([0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$";

        var match = Regex.Match(firmwareVersion, regex, RegexOptions.IgnoreCase);
        return match.Success;
    }
    private async Task<Device> GetDevice(string serialNumber, string sharedSecret)
    {
        var device = await _smartAcContext.Devices
            .Where(device => device.SerialNumber == serialNumber && device.SharedSecret == sharedSecret)
            .FirstOrDefaultAsync();
        return device ??= new Device();
    }
    private async Task ActivateDeviceRegistration(Device device, string firmwareVersion, string tokenId)
    {
        var newRegistrationDevice = new DeviceRegistration()
        {
            Device = device,
            TokenId = tokenId
        };

        await _smartAcContext.DeviceRegistrations.DeactivateRegistrations(device.SerialNumber);
        await _smartAcContext.DeviceRegistrations.AddAsync(newRegistrationDevice);
        device.UpdateDeviceDetails(firmwareVersion, newRegistrationDevice);

        await _smartAcContext.SaveChangesAsync();

        _logger.LogDebug(
            "A new registration record with tokenId \"{tokenId}\" has been created for the device \"{serialNumber}\"",
            device.SerialNumber, tokenId);
    }

    #endregion
}
