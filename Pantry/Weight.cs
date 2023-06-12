namespace Pantry
{
    using System.Globalization;

    /// <summary>
    /// Represents weight in gramms.
    /// </summary>
    public class MetricWeightRus : MetricUnit
    {
        public MetricWeightRus() : base(CultureInfo.GetCultureInfo("ru-RU"))
        {
            this.Symbols = new[] { "гр", "г", "грам", "грамм", "граммов" };
            this.KiloSymbols = new[] { "кг", "килограм", "килограмм", "килограммов" };
        }

        public override string Name => "Вес";
        public override string Symbol => "гр";
        public override MeasurementType Type => MeasurementType.Weight;
    }

    /// <summary>
    /// Represents weight in gramms.
    /// </summary>
    public class MetricWeightEng : MetricUnit
    {
        public MetricWeightEng() : base(CultureInfo.GetCultureInfo("en-US"))
        {
            this.Symbols = new[] { "gr", "g", "gram", "grams", "gramme", "grammes", };
            this.KiloSymbols = new[] { "kg", "kilogram", "kilograms", "kilogramme", "kilogrammes" };
        }

        public override string Name => "Weight";
        public override string Symbol => "gr";
        public override MeasurementType Type => MeasurementType.Weight;
    }
}