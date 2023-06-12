namespace Pantry
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    public enum CurrencySymbolPlacement
    {
        BeforeSum,
        AfterSum
    }

    [JsonConverter(typeof(JsonConverter))]
    public readonly struct Money : IEquatable<Money>, IComparable<Money>
    {
        private class JsonConverter : JsonConverter<Money>
        {
            public override Money Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
                Parse(reader.GetString());

            public override void Write(Utf8JsonWriter writer, Money value, JsonSerializerOptions options) =>
                writer.WriteStringValue(value);
        }

        public Money(decimal sum, Currency currency)
        {
            this.Sum = sum;
            this.Currency = currency;
        }

        public decimal Sum { get; }

        public Currency Currency { get; }

        private bool IsNothing => this.Currency is null;

        public static bool TryParse(ReadOnlySpan<char> writing, out Money money) =>
            Currency.TryParseMoneyExact(writing, out money);

        public static bool TryParse(ReadOnlySpan<char> writing, Currency currency, out Money money) =>
            currency is null ? Currency.TryParseMoneyExact(writing, out money) : currency.TryParseMoney(writing, out money);

        public static Money Parse(ReadOnlySpan<char> writing, Currency currency = null) =>
            currency is null ? Currency.ParseMoneyExact(writing) : currency.ParseMoney(writing);

        public static Money operator +(Money left, Money right)
        {
            if (left.IsNothing)
                return right;
            if (right.IsNothing)
                return left;

            if (left.Currency != right.Currency)
                throw new InvalidOperationException();

            return new Money(left.Sum + right.Sum, left.Currency);
        }

        public static Money operator -(Money left, Money right)
        {
            if (right.IsNothing)
                return left;
            if (left.IsNothing)
                return new Money(-right.Sum, right.Currency);

            if (left.Currency != right.Currency)
                throw new InvalidOperationException();

            return new Money(left.Sum - right.Sum, left.Currency);
        }

        public static bool operator ==(Money left, Money right) => left.Equals(right);

        public static bool operator !=(Money left, Money right) => !left.Equals(right);

        public static bool operator >(Money left, Money right) => left.CompareTo(right) > 0;

        public static bool operator >=(Money left, Money right) => left.CompareTo(right) >= 0;

        public static bool operator <(Money left, Money right) => left.CompareTo(right) < 0;

        public static bool operator <=(Money left, Money right) => left.CompareTo(right) <= 0;

        public static implicit operator string(Money sum) => sum.ToString();

        public static implicit operator Money(string sum) => Parse(sum);

        public static implicit operator Money(ReadOnlySpan<char> sum) => Parse(sum);

        public static implicit operator Money(decimal sum) => Currency.GetMoney(sum, ReadOnlySpan<char>.Empty);

        public override string ToString()
        {
            if (this.IsNothing)
                return String.Empty;

            return this.Currency.ToString(this.Sum);
        }

        public override int GetHashCode() => HashCode.Combine(this.Sum, this.Currency);

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, null))
                return false;

            if (obj is Money other)
                return Equals(other);

            return false;
        }

        public int CompareTo(Money other)
        {
            if (this.IsNothing)
                return -1;
            if (other.IsNothing)
                return 1;

            if (this.Currency != other.Currency)
                return this.Currency.CompareTo(other.Currency);

            return this.Sum.CompareTo(other.Sum);
        }

        public bool Equals(Money other)
        {
            return
                this.Currency == other.Currency &&
                (this.Sum == other.Sum || this.IsNothing);
        }
    }

    public static class MoneyExtensions
    {
        public static Money Sum(this IEnumerable<Money> source)
        {
            //todo: find the currency and the sum in one pass
            var currency = source
                .Select(m => m.Currency)
                .Distinct()
                .SingleOrDefault();

            if (currency == null)
                return default;

            return new Money(source.Select(m => m.Sum).Sum(), currency);
        }

        public static Money Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, Money> selector)
        {
            return Sum(source.Select(m => selector(m)).ToArray()); //todo: remove memory allocation
        }
    }
}