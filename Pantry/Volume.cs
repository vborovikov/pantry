namespace Pantry
{
    using System.Globalization;

    /// <summary>
    /// Represents volume in millilitres.
    /// </summary>
    public class MetricVolumeRus : MetricUnit
    {
        public MetricVolumeRus() : base(CultureInfo.GetCultureInfo("ru-RU"))
        {
            this.Symbols = new[] { "мл", "миллилитр", "миллилитра", "миллилитров" };
            this.KiloSymbols = new[] { "л", "литр", "литра", "литров" };
        }

        public override string Name => "Объём";
        public override string Symbol => "мл";
        public override MeasurementType Type => MeasurementType.Volume;
    }

    /// <summary>
    /// Represents volume in millilitres.
    /// </summary>
    public class MetricVolumeEng : MetricUnit
    {
        public MetricVolumeEng() : base(CultureInfo.GetCultureInfo("en-US"))
        {
            this.Symbols = new[] { "mL", "milliliter", "milliliters", "millilitre", "millilitres" };
            this.KiloSymbols = new[] { "L", "liter", "liters", "litre", "litres" };
        }

        public override string Name => "Volume";
        public override string Symbol => "ml";
        public override MeasurementType Type => MeasurementType.Volume;
    }
}