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

        public override string ToString(Measure measure, IFormatProvider formatProvider)
        {
            var value = measure.Value.Value;
            if (value > 1000f && value % 1000f == 0f)
            {
                value /= 1000f;
                return $"{value.ToString(formatProvider)} {this.KiloSymbols[^1]}";
            }

            return base.ToString(measure, formatProvider);
        }
    }
}