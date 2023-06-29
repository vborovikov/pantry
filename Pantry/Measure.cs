namespace Pantry
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents item quantity.
    /// </summary>
    [JsonConverter(typeof(JsonConverter))]
    public readonly struct Measure : IEquatable<Measure>, IEquatable<string>, IComparable<Measure>, IComparable<string>,
        ISpanFormattable, ISpanParsable<Measure>, IFiniteSpanParsable<Measure>
    {
        private sealed class JsonConverter : JsonConverter<Measure>
        {
            private const int MaxStringLength = 50;

            public override Measure Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.Null)
                {
                    return default;
                }

                Span<char> buffer = stackalloc char[MaxStringLength];
                var charsWritten = reader.CopyString(buffer);
                ReadOnlySpan<char> source = buffer[..charsWritten];
                return Parse(source, null);
            }


            public override void Write(Utf8JsonWriter writer, Measure value, JsonSerializerOptions options)
            {
                Span<char> buffer = stackalloc char[MaxStringLength];
                value.TryFormat(buffer, out var charsWritten, ReadOnlySpan<char>.Empty, null);
                ReadOnlySpan<char> source = buffer[..charsWritten];
                writer.WriteStringValue(source);
            }
        }

        //public Measure()
        //    : this(default, MeasureUnit.Number)
        //{
        //}

        public Measure(Fractional value, MeasureUnit unit)
        {
            this.Value = value;
            this.Unit = unit;
        }

        public Fractional Value { get; }

        public MeasureUnit Unit { get; }

        public static Measure Parse(string s, IFormatProvider? provider) =>
            Parse(s.AsSpan(), provider);

        public static bool TryParse(string? s, IFormatProvider? provider, out Measure result) =>
            TryParse(s.AsSpan(), provider, out result);

        public static Measure Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
        {
            if (TryParse(s, provider, out var result, out _))
            {
                return result;
            }

            throw new FormatException();
        }

        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Measure result) =>
            TryParse(s, provider, out result, out _);

        public static Measure Parse(ReadOnlySpan<char> span) => Parse(span, null);

        public static bool TryParse(ReadOnlySpan<char> span, out Measure result) => TryParse(span, null, out result);

        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out Measure result, out int charsConsumed)
        {
            if (s.IsEmpty || s.IsWhiteSpace())
            {
                result = default;
                charsConsumed = 0;
                return false;
            }

            if (!Fractional.TryParse(s, provider, out var value, out var valueLength))
            {
                return Number.TryParse(s, provider, out result, out charsConsumed);
            }

            result = MeasureUnit.GetMeasure(value, s[valueLength..], provider, out var unitLength);
            charsConsumed = valueLength + unitLength;
            return true;
        }

        public static implicit operator Measure(string quantity)
        {
            return Measure.Parse(quantity);
        }

        public static implicit operator Measure(int number)
        {
            if (number < 0)
                throw new OverflowException();

            return new Measure(number, MeasureUnit.Number);
        }

        public static implicit operator string(Measure quantity)
        {
            return quantity.ToString();
        }

        public static Measure operator /(Measure measure, int count)
        {
            return new Measure(measure.Value / (float)count, measure.Unit);
        }

        public static Fractional operator /(Measure left, Measure right)
        {
            if (left.Unit != right.Unit)
                throw new InvalidOperationException();

            return left.Value / right.Value;
        }

        public static Measure operator *(Measure quantity, float rate)
        {
            return new Measure(quantity.Value * rate, quantity.Unit);
        }

        public static Measure operator -(Measure left, Measure right)
        {
            // Can't take quantities in different units.
            // Actually we can but we need a conversion method for that...
            if (left.Unit != right.Unit)
                throw new InvalidOperationException();

            // impossibru! Can't take more than available
            if (left.Value < right.Value)
                throw new InvalidOperationException();

            return new Measure(left.Value - right.Value, left.Unit);
        }

        public static bool operator ==(Measure left, Measure right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Measure left, Measure right)
        {
            return left.Equals(right) == false;
        }

        public static bool operator >(Measure left, Measure right)
        {
            return left.CompareTo(right) == 1;
        }

        public static bool operator <(Measure left, Measure right)
        {
            return left.CompareTo(right) == -1;
        }

        public static bool operator >=(Measure left, Measure right)
        {
            return left.CompareTo(right) >= 0;
        }

        public static bool operator <=(Measure left, Measure right)
        {
            return left.CompareTo(right) <= 0;
        }

        public override string ToString() => ToString(null, CultureInfo.CurrentCulture);

        public string ToString(string? format, IFormatProvider? formatProvider) =>
            this.Unit?.ToString(this, format, formatProvider) ?? String.Empty;

        public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
        {
            if (this.Unit is null)
            {
                charsWritten = 0;
                return false;
            }

            return this.Unit.TryFormat(this, destination, out charsWritten, format, provider);
        }

        public override bool Equals(object? obj)
        {
            if (Object.ReferenceEquals(obj, null))
                return false;

            if (obj is Measure other)
                return Equals(other);

            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.Value, this.Unit);
        }

        public bool Equals(Measure other)
        {
            return this.Value == other.Value &&
                   this.Unit == other.Unit;
        }

        public bool Equals(string? other)
        {
            if (String.IsNullOrWhiteSpace(other))
                return false;

            return Equals(Parse(other));
        }

        public int CompareTo(Measure other)
        {
            if (this.Value == default)
                return -1;
            if (other.Value == default)
                return 1;

            if (other.Unit != this.Unit)
                throw new InvalidOperationException();

            // The quantity comparison depends on the comparison of
            // the underlying Fractional values.
            return this.Value.CompareTo(other.Value);
        }

        public int CompareTo(string? other)
        {
            // If other is not a valid object reference, this instance is greater.
            if (String.IsNullOrWhiteSpace(other))
                return 1;

            return CompareTo(Parse(other));
        }
    }
}