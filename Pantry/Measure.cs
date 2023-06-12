namespace Pantry
{
    using System;
    using System.Globalization;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents item quantity.
    /// </summary>
    [JsonConverter(typeof(JsonConverter))]
    public readonly struct Measure :
        IEquatable<Measure>, IEquatable<string>,
        IComparable<Measure>, IComparable<string>
    {
        private sealed class JsonConverter : JsonConverter<Measure>
        {
            public override Measure Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
                TryParse(reader.GetString(), out var measure) ? measure : default;

            public override void Write(Utf8JsonWriter writer, Measure value, JsonSerializerOptions options) =>
                writer.WriteStringValue(value);
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

        public static Measure Parse(ReadOnlySpan<char> span, IFormatProvider formatProvider = null)
        {
            if (!TryParse(span, formatProvider, out var result))
            {
                throw new FormatException();
            }

            return result;
        }

        public static bool TryParse(ReadOnlySpan<char> span, out Measure result) => TryParse(span, null, out result);

        public static bool TryParse(ReadOnlySpan<char> span, IFormatProvider formatProvider, out Measure result) =>
            TryParseRaw(span, formatProvider, out result) > 0;

        public static bool TryParse(string str, IFormatProvider formatProvider, out Measure result) =>
            TryParse(str.AsSpan(), formatProvider, out result);

        public static int TryParseRaw(ReadOnlySpan<char> span, IFormatProvider formatProvider, out Measure result)
        {
            if (span.IsEmpty || span.IsWhiteSpace())
            {
                result = default;
                return 0;
            }

            var valueLength = Fractional.TryParseRaw(span, formatProvider, out var value);
            if (valueLength <= 0)
            {
                return MeasureUnit.Number.TryParseRaw(span, formatProvider, out result);
            }

            result = MeasureUnit.GetMeasure(value, span[valueLength..], formatProvider, out var unitLength);
            return valueLength + unitLength;
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

        public override string ToString() =>
            this.Unit?.ToString(this, CultureInfo.CurrentCulture) ?? String.Empty;

        public string ToString(IFormatProvider formatProvider) =>
            this.Unit?.ToString(this, formatProvider) ?? String.Empty;

        public override bool Equals(object obj)
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

        public bool Equals(string other)
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

        public int CompareTo(string other)
        {
            // If other is not a valid object reference, this instance is greater.
            if (String.IsNullOrWhiteSpace(other))
                return 1;

            return CompareTo(Parse(other));
        }
    }
}