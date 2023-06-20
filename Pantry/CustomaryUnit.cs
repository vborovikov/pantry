namespace Pantry
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    public abstract class CustomaryUnit : MeasureUnit
    {
        private record Conversion(float Factor, float MinThreshold, float MaxThreshold, string Symbol, string ManySymbol);

        private readonly Dictionary<string[], Conversion> conversionTable;

        protected CustomaryUnit() : base(CultureInfo.GetCultureInfo("en-US"))
        {
            this.conversionTable = new Dictionary<string[], Conversion>();
        }

        protected void Add(string[] symbols, float factor, 
            float minThreshold = default, float maxThreshold = default,
            string? symbol = default, string? manySymbol = default)
        {
            //var minThreshold = this.conversionTable.Values.Select(c => c.Factor).Order().LastOrDefault(v => v < factor, factor / 2f);
            //var conversion = new Conversion(factor, minThreshold, factor * 4f, symbols[^2], symbols[^1]);

            var conversion = new Conversion(factor, minThreshold, maxThreshold,
                symbol ?? symbols[^2], manySymbol ?? symbols[^1]);

            SortSymbols(symbols);
            this.conversionTable.Add(symbols, conversion);
        }

        protected sealed override int TryGetMeasure(Fractional value, ReadOnlySpan<char> unitSpan, out Measure result)
        {
            foreach (var conversion in this.conversionTable)
            {
                var charsConsumed = TryGetMeasure(value, unitSpan, conversion.Key, out var measure);
                if (charsConsumed > 0)
                {
                    result = measure * conversion.Value.Factor;
                    return charsConsumed;
                }
            }

            result = default;
            return 0;
        }

        public override string ToString(in Measure measure, string? format, IFormatProvider? formatProvider)
        {
            var value = measure.Value.Value;
            var conversion = default(Conversion);
            foreach (var c in this.conversionTable.Values) // assuming conversions are sorted by Factor
            {
                if (value >= c.MinThreshold && value < c.MaxThreshold)
                {
                    conversion = c;
                    var convertedValue = measure.Value / c.Factor;
                    if (convertedValue.IsInteger)
                    {
                        break;
                    }
                }
            }

            if (conversion is not null)
            {
                var measureValue = measure.Value / conversion.Factor;
                return measureValue.Value > 1f ?
                    $"{measureValue.ToString(format, formatProvider)} {conversion.ManySymbol}" :
                    $"{measureValue.ToString(format, formatProvider)} {conversion.Symbol}";
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
