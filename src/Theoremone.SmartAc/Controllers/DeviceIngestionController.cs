using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Theoremone.SmartAc.Application.DTOs;
using Theoremone.SmartAc.Application.Interfaces;
using Theoremone.SmartAc.Application.Models;
using Theoremone.SmartAc.Bindings;

namespace Theoremone.SmartAc.Controllers;

[ApiController]
[Route("api/v1/device")]
[Authorize("DeviceIngestion")]
public class DeviceIngestionController : ControllerBase
{
    private readonly IDeviceService _deviceService;
    private readonly IAlertService _alertService;

    public DeviceIngestionController(
        IDeviceService deviceService,
        IAlertService alertService
        )
    {
        _deviceService = deviceService;
        _alertService = alertService;
    }

    /// <summary>
    /// Allow smart ac devices to register themselves  
    /// and get a jwt token for subsequent operations
    /// </summary>
    /// <param name="serialNumber">Unique device identifier burned into ROM</param>
    /// <param name="sharedSecret">Unique device shareable secret burned into ROM</param>
    /// <param name="firmwareVersion">Device firmware version at the moment of registering</param>
    /// <returns>A jwt token</returns>
    /// <response code="400">If any of the required data is not pressent or is invalid.</response>
    /// <response code="401">If something is wrong on the information provided.</response>
    /// <response code="200">If the registration has sucesfully generated a new jwt token.</response>
    [HttpPost("{serialNumber}/registration")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RegisterDevice(
        [Required][FromRoute] string serialNumber,
        [Required][FromHeader(Name = "x-device-shared-secret")] string sharedSecret,
        [Required][FromQuery] string firmwareVersion)
    {
        // TODO: The firmware version should have semantic versioning format as per BE-DEV-1, the tests are correct but
        //       I think someone forgot to validate it here.
        //INFO: The validation is done in the service layer

        return Ok(await _deviceService.RegisterDeviceForAuth(new RegisterDeviceDto(serialNumber, sharedSecret, firmwareVersion)));
    }

    /// <summary>
    /// Allow smart ac devices to send sensor readings in batch
    /// 
    /// This will additionally trigger analysis over the sensor readings
    /// to generate alerts based on it
    /// </summary>
    /// <param name="serialNumber">Unique device identifier burned into ROM.</param>
    /// <param name="sensorReadings">Collection of sensor readings send by a device.</param>
    /// <response code="401">If jwt token provided is invalid.</response>
    /// <response code="202">If sensor readings has sucesfully accepted.</response>
    /// <returns>No Content.</returns>
    [HttpPost("readings/batch")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> AddSensorReadings(
        [ModelBinder(BinderType = typeof(DeviceInfoBinder))] string serialNumber,
        [FromBody] IEnumerable<DeviceReadingRecord> sensorReadings)
    {
        var deviceReadingRecordAndValidationErrorsList = await _deviceService.RetrieveValidReadingsAndThrowIfInvalid(sensorReadings, serialNumber);

        if (deviceReadingRecordAndValidationErrorsList.Count > 0)
        {
            await _alertService.ProduceAlerts(serialNumber, deviceReadingRecordAndValidationErrorsList);
        }
        await _deviceService.AddDeviceSensorReadings(serialNumber, sensorReadings);
        return Accepted();
    }


    
    /// <summary>
    /// Allow devices read server-side created alerts, so that they can display on their user interfaces to their users or maintenance crews.
    /// </summary>
    /// <param name="serialNumber">Unique device identifier burned into ROM</param>
    /// <param name="deviceParameters">Query parameters for filtering and paging such as State, PageSize and PageNumber</param>
    /// <returns></returns>
    [HttpGet("readings")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(IEnumerable<DeviceAlertDto>),StatusCodes.Status200OK)]
    public async Task<IActionResult> DeviceAlertReadings(
        [ModelBinder(BinderType = typeof(DeviceInfoBinder))] string serialNumber,
        [FromQuery] DeviceParameters deviceParameters)
    {
        var (readings, metaData) = await _deviceService.GetDeviceAlertReadingsAsync(serialNumber, deviceParameters);
        Response.Headers.Add("X-Pagination", metaData);
        return Ok(readings);
    }
}