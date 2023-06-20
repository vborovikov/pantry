namespace Pantry
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    public enum MeasurementType
    {
        Count,
        Weight,
        Volume,
        Length,
        Duration,
        Percentage,

        CNT = Count,
        WGT = Weight,
        VOL = Volume,
        LEN = Length,
        DUR = Duration,
        PCT = Percentage,
    }

    public abstract class MeasureUnit : IEquatable<MeasureUnit>
    {
        private static readonly IReadOnlyList<MeasureUnit> KnownUnits = new MeasureUnit[]
        {
            new MetricVolumeEng(),
            new MetricVolumeRus(),
            new MetricWeightEng(),
            new MetricWeightRus(),
            new CustomaryVolume(),
            new CustomaryWeight(),
            new Percent(),
            new Number(),
        };

        private readonly CultureInfo culture;

        protected MeasureUnit(CultureInfo culture)
        {
            this.culture = culture;
        }

        public static Number Number => (Number)KnownUnits[^1];

        public abstract string Name { get; }

        public abstract string Symbol { get; }

        public abstract MeasurementType Type { get; }

        public static Measure GetMeasure(Fractional value, ReadOnlySpan<char> unitSpan, IFormatProvider? formatProvider, out int charsConsumed)
        {
            var unitNormalized = unitSpan.TrimStart();
            var charsTrimmed = unitSpan.Length - unitNormalized.Length;

            if (!unitNormalized.IsEmpty)
            {
                var anyCulture = formatProvider is null || CultureInfo.InvariantCulture.Equals(formatProvider);
                var twoLetterISOLanguageName = (formatProvider as CultureInfo)?.TwoLetterISOLanguageName;

                do
                {
                    foreach (var unit in KnownUnits)
                    {
                        if (anyCulture ||
                            unit.culture.Equals(CultureInfo.InvariantCulture) ||
                            unit.culture.Equals(formatProvider) ||
                            unit.culture.TwoLetterISOLanguageName == twoLetterISOLanguageName)
                        {
                            //todo: pass here formatProvider so Number can use it to find the right unit
                            charsConsumed = unit.TryGetMeasure(value, unitNormalized, out var result);
                            if (charsConsumed > 0)
                            {
                                charsConsumed += charsTrimmed;
                                return result;
                            }
                        }
                    }

                    // try again with all cultures
                    anyCulture = !anyCulture;
                } while (anyCulture);
            }

            charsConsumed = 0;
            return new Measure(value, Number);
        }

        public static bool operator !=(MeasureUnit left, MeasureUnit right)
        {
            return !(left == right);
        }

        public static bool operator ==(MeasureUnit left, MeasureUnit right)
        {
            if (Object.ReferenceEquals(left, right))
                return true;

            if (left is null)
                return false;

            return left.Equals(right);
        }

        public override string ToString() => $"{this.Name} ({this.Symbol})";

        public virtual string ToString(in Measure measure, string? format, IFormatProvider? formatProvider) =>
            $"{measure.Value} {this.Symbol}";

        public virtual bool TryFormat(in Measure measure, Span<char> destination, out int charsWritten, 
            ReadOnlySpan<char> format, IFormatProvider? provider)
        {
            charsWritten = 0;

            if (!measure.Value.TryFormat(destination, out var valueCharsWritten, format, provider))
                return false;
            charsWritten += valueCharsWritten;

            if (charsWritten == destination.Length)
                return false;
            destination[charsWritten++] = ' ';

            if (!this.Symbol.TryCopyTo(destination[charsWritten..]))
                return false;
            charsWritten += this.Symbol.Length;

            return true;
        }

        public sealed override bool Equals(object? obj)
        {
            var other = obj as MeasureUnit;
            return Equals(other);
        }

        public bool Equals(MeasureUnit? other)
        {
            if (other is null)
                return false;

            return
                String.Equals(this.Name, other.Name, StringComparison.Ordinal) &&
                String.Equals(this.Symbol, other.Symbol, StringComparison.Ordinal) &&
                this.culture.Equals(other.culture);
        }

        public sealed override int GetHashCode()
        {
            return HashCode.Combine(this.Name, this.Symbol, this.culture);
        }

        public bool TryParseMeasure(ReadOnlySpan<char> span, out Measure result)
        {
            if (span.IsEmpty || span.IsWhiteSpace())
            {
                result = default;
                return false;
            }

            if (!Fractional.TryParse(span, this.culture, out var value, out var valueLength))
            {
                result = default;
                return false;
            }

            var unitSpan = span[valueLength..];
            if (unitSpan.IsEmpty)
            {
                result = new Measure(value, this);
                return true;
            }

            return TryGetMeasure(value, unitSpan, out result) > 0;
        }

        protected virtual int TryGetMeasure(Fractional value, ReadOnlySpan<char> unitSpan, out Measure result) =>
            TryGetMeasure(value, unitSpan, this.Symbol, out result);

        protected int TryGetMeasure(Fractional value, ReadOnlySpan<char> unitSpan, string[] symbols, out Measure result)
        {
            foreach (var symbol in symbols)
            {
                var charsConsumed = TryGetMeasure(value, unitSpan, symbol, out result);
                if (charsConsumed > 0)
                {
                    return charsConsumed;
                }
            }

            result = default;
            return 0;
        }

        protected int TryGetMeasure(Fractional value, ReadOnlySpan<char> unitSpan, ReadOnlySpan<char> symbol, out Measure result)
        {
            var charsScanned = 0;
            if (symbol.IndexOf(' ') > 0)
            {
                // compare span and symbol ignoring punctuation and whitespace
                charsScanned = ScanSpanRelaxed(unitSpan, symbol);
            }
            else
            {
                // single char symbol has case-sensitive comparison method
                var stringComparison =
                    symbol.Length == 1 /*&& char.IsUpper(symbol[0])*/ ?
                    StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

                if (unitSpan.StartsWith(symbol, stringComparison))
                {
                    charsScanned = symbol.Length;
                }
            }

            if (charsScanned > 0)
            {
                var unitSeparated = true;
                if (unitSpan.Length > charsScanned)
                {
                    var rest = unitSpan.Slice(charsScanned, 1);
                    unitSeparated = rest.IsWhiteSpace() ||
                        char.IsPunctuation(rest[0]) ||
                        char.IsSeparator(rest[0]) ||
                        char.IsSymbol(rest[0]);
                    if (unitSeparated)
                    {
                        ++charsScanned;
                    }
                }

                if (unitSeparated)
                {
                    result = new Measure(value, this);
                    return charsScanned;
                }
            }

            result = default;
            return 0;
        }

        private int ScanSpanRelaxed(ReadOnlySpan<char> span, ReadOnlySpan<char> symbol)
        {
            var symbolPos = 0;
            var spanPos = 0;
            for (; symbolPos != symbol.Length; ++symbolPos)
            {
                var canSkip = char.IsWhiteSpace(symbol[symbolPos]);
                for (; spanPos < span.Length; spanPos++)
                {
                    if (char.ToUpper(span[spanPos], this.culture) == char.ToUpper(symbol[symbolPos], this.culture))
                    {
                        if (!canSkip)
                        {
                            ++spanPos;
                            break;
                        }
                    }

                    if (!canSkip)
                        return 0;

                    if (!char.IsWhiteSpace(span[spanPos]) &&
                        !char.IsPunctuation(span[spanPos]) &&
                        !char.IsSeparator(span[spanPos]))
                    {
                        if (canSkip)
                            break;

                        return 0;
                    }
                }
            }

            return spanPos >= symbolPos ? spanPos : 0;
        }

        protected static string[] SortSymbols(string[] symbols)
        {
            Array.Sort(symbols, (x, y) => -x.Length.CompareTo(y.Length));
            return symbols;
        }
    }
}