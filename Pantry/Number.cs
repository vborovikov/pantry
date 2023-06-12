namespace Pantry
{
    using System;
    using System.Globalization;

    /// <summary>
    /// Represents quantity in pieces.
    /// </summary>
    public sealed class Number : MeasureUnit
    {
        public Number() : base(CultureInfo.InvariantCulture)
        {
        }

        public override string Name => String.Empty;
        public override string Symbol => String.Empty;
        public override MeasurementType Type => MeasurementType.Count;

        public override string ToString() => "#";

        public override string ToString(Measure measure, IFormatProvider formatProvider)
        {
            return (formatProvider as CultureInfo) switch
            {
                { TwoLetterISOLanguageName: "en" } => $"{measure.Value} pcs",
                { TwoLetterISOLanguageName: "ru" } => $"{measure.Value} шт",
                _ => measure.Value.ToString()
            };
        }

        public int TryParseRaw(ReadOnlySpan<char> span, IFormatProvider formatProvider, out Measure result)
        {
            //todo: parse number words to get Fractional, then parse the unit using GetMeasure.

            // 1 large pkg
            // 1 piece
            // 2 cans

            result = default;
            return 0;
        }
    }
}