namespace Pantry
{
    using System;
    using System.Globalization;

    /// <summary>
    /// Represents quantity as percentage.
    /// </summary>
    public sealed class Percent : MeasureUnit
    {
        private const string PercentSymbol = "%";

        public Percent() : base(CultureInfo.InvariantCulture)
        {
        }

        public override string Name => String.Empty;
        public override string Symbol => PercentSymbol;
        public override MeasurementType Type => MeasurementType.Percentage;

        public override string ToString() => this.Symbol;

        public override string ToString(in Measure measure, string? format, IFormatProvider? formatProvider) =>
            String.Concat(measure.Value.ToString(format, formatProvider), PercentSymbol);

        public override bool TryFormat(in Measure measure, Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
        {
            charsWritten = 0;
            if (!measure.Value.TryFormat(destination, out var valueCharsWritten, format, provider))
                return false;
            charsWritten += valueCharsWritten;

            if (!PercentSymbol.TryCopyTo(destination[charsWritten..]))
                return false;
            ++charsWritten;

            return true;
        }
    }
}