namespace Pantry
{
    using System;
    using System.Globalization;

    public abstract class MetricUnit : MeasureUnit
    {
        private string[] symbols;
        private string[] kiloSymbols;

        protected MetricUnit(CultureInfo culture) : base(culture)
        {
            this.symbols = Array.Empty<string>();
            this.kiloSymbols = Array.Empty<string>();
        }

        protected string[] Symbols
        {
            get => this.symbols;
            set => this.symbols = SortSymbols(value);
        }

        protected string[] KiloSymbols
        {
            get => this.kiloSymbols;
            set => this.kiloSymbols = SortSymbols(value);
        }

        protected sealed override int TryGetMeasure(Fractional value, ReadOnlySpan<char> unitSpan, out Measure result)
        {
            var charsConsumed = TryGetMeasure(value, unitSpan, this.Symbols, out result);
            if (charsConsumed > 0)
            {
                return charsConsumed;
            }

            charsConsumed = TryGetMeasure(value, unitSpan, this.KiloSymbols, out var measure);
            if (charsConsumed > 0)
            {
                result = measure * 1000f;
                return charsConsumed;
            }

            result = default;
            return 0;
        }

        public override string ToString(in Measure measure, string? format, IFormatProvider? formatProvider)
        {
            var value = measure.Value.Value;
            if (value > 1000f && value % 1000f == 0f)
            {
                value /= 1000f;
                return $"{value.ToString(format, formatProvider)} {this.KiloSymbols[^1]}";
            }

            return base.ToString(measure, format, formatProvider);
        }

        public override bool TryFormat(in Measure measure, Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
        {
            //todo: implement
            return base.TryFormat(measure, destination, out charsWritten, format, provider);
        }
    }
}