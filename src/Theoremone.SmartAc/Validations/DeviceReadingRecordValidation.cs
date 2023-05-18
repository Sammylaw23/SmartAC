using FluentValidation;
using Microsoft.Extensions.Options;
using Theoremone.SmartAc.Application.Models;
using Theoremone.SmartAc.Application.Settings;
using Theoremone.SmartAc.Domain.Models;

namespace Theoremone.SmartAc.Validations
{

    public class DeviceReadingRecordValidation : AbstractValidator<DeviceReadingRecord>
    {
        private readonly SensorLimitsOptions _limit;
        public DeviceReadingRecordValidation(IOptionsMonitor<SensorLimitsOptions> sensorLimitConfig)
        {
            _limit = sensorLimitConfig.CurrentValue;

            RuleFor(x => x.Temperature).InclusiveBetween(_limit.LowerTemperature, _limit.HigherTemperature)
                .WithMessage("Sensor {PropertyName} reported out of range value");
            RuleFor(x => x.Humidity).InclusiveBetween(_limit.LowerHumidity, _limit.HigherHumidity)
                .WithMessage("Sensor {PropertyName} reported out of range value");
            RuleFor(x => x.CarbonMonoxide)
                .InclusiveBetween(_limit.LowerCarbonMonoxide, _limit.HigherCarbonMonoxide).WithMessage("Sensor {PropertyName} reported out of range value")
                .LessThanOrEqualTo(_limit.UnsafeCOThreshold).WithMessage("CO value has exceeded danger limit");

            RuleFor(x => x.Health).Must(x => x.Equals(DeviceHealth.Ok)).WithMessage("Device is reporting health problem: {PropertyValue}");
        }
    }
}
