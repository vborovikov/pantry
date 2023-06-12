namespace Pantry
{
    using System;
    using System.Globalization;

    /// <summary>
    /// Represents quantity as percentage.
    /// </summary>
    public sealed class Percent : MeasureUnit
    {
        public Percent() : base(CultureInfo.InvariantCulture)
        {
        }

        public override string Name => String.Empty;
        public override string Symbol => "%";
        public override MeasurementType Type => MeasurementType.Percentage;
        public override string ToString() => this.Symbol;
        public override string ToString(Measure measure, IFormatProvider formatProvider) =>
            String.Concat(measure.Value, this.Symbol);
    }
}