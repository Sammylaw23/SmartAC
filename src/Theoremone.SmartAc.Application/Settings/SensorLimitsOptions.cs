namespace Theoremone.SmartAc.Application.Settings
{
    public class SensorLimitsOptions
    {
        public const string SensorLimits = "SensorLimits";

        public decimal LowerTemperature { get; set; }
        public decimal HigherTemperature { get; set; }
        public decimal LowerHumidity { get; set; }
        public decimal HigherHumidity { get; set; }
        public decimal LowerCarbonMonoxide { get; set; }
        public decimal HigherCarbonMonoxide { get; set; }
        public decimal UnsafeCOThreshold { get; set; }
       
    }


}
