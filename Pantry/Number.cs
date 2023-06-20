namespace Pantry
{
    using System;
    using System.Globalization;

    /// <summary>
    /// Represents quantity in pieces.
    /// </summary>
    public sealed class Number : MeasureUnit
    {
        private const string NumberSymbol = "-";

        public Number() : base(CultureInfo.InvariantCulture)
        {
        }

        public override string Name => String.Empty;
        public override string Symbol => NumberSymbol;
        public override MeasurementType Type => MeasurementType.Count;

        public override string ToString() => "#";

        public override string ToString(in Measure measure, string? format, IFormatProvider? formatProvider)
        {
            if (formatProvider is null)
                return String.Concat(measure.Value.ToString(format!), NumberSymbol);

            return (formatProvider as CultureInfo) switch
            {
                { TwoLetterISOLanguageName: "en" } => $"{measure.Value.ToString(format, formatProvider)} pcs",
                { TwoLetterISOLanguageName: "ru" } => $"{measure.Value.ToString(format, formatProvider)} шт",
                _ => measure.Value.ToString()
            };
        }

        public override bool TryFormat(in Measure measure, Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
        {
            charsWritten = 0;
            if (!measure.Value.TryFormat(destination, out var valueCharsWritten, format, provider))
                return false;
            charsWritten += valueCharsWritten;

            if (!NumberSymbol.TryCopyTo(destination[charsWritten..]))
                return false;
            ++charsWritten;

            return true;
        }

        public bool TryParse(ReadOnlySpan<char> span, IFormatProvider? formatProvider, out Measure result, out int charsConsumed)
        {
            //todo: parse number words to get Fractional, then parse the unit using GetMeasure.

            // one large pkg
            // two eggs
            // a hundred cans of beer

            result = default;
            charsConsumed = 0;
            return false;
        }
    }
}