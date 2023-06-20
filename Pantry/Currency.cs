namespace Pantry
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    public abstract class Currency : IEquatable<Currency>, IComparable<Currency>
    {
        private enum NotationCategory
        {
            None,
            Sum,
            Unit,
        }

        private readonly ref struct NotationSpan
        {
            public NotationSpan(ReadOnlySpan<char> span, NotationCategory category)
            {
                this.Span = span;
                this.Category = category;
            }

            public ReadOnlySpan<char> Span { get; }
            public NotationCategory Category { get; }

            public static implicit operator ReadOnlySpan<char>(NotationSpan note) => note.Span;
        }

        private ref struct NotationEnumerator
        {
            private readonly Currency currency;
            private ReadOnlySpan<char> writing;

            public NotationEnumerator(Currency currency, ReadOnlySpan<char> writing)
            {
                this.currency = currency;
                this.writing = writing.Trim();
                this.Current = default;
            }

            public NotationSpan Current { get; private set; }

            public NotationEnumerator GetEnumerator() => this;

            public bool MoveNext()
            {
                if (this.writing.IsEmpty)
                    return false;

                var category = NotationCategory.Sum;
                var endPos = 0;
                if (currency.numericChars.Contains(this.writing[0]))
                {
                    // sum
                    endPos = this.writing.IndexOfAnyExcept(currency.numericChars);
                }
                else
                {
                    // unit
                    category = NotationCategory.Unit;
                    endPos = this.writing.IndexOfAny(currency.numericChars);
                }
                if (endPos < 0)
                    endPos = this.writing.Length;

                this.Current = new NotationSpan(this.writing[..endPos], category);
                this.writing = this.writing[endPos..];

                return true;
            }
        }

        private const char NormalSpaceChar = '\x20';
        private const string SpaceChars = "\t\xA0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200A\u202F\u205F\u3000";
        private const StringComparison CodeComparison = StringComparison.OrdinalIgnoreCase;
        private const StringComparison WritingComparison = StringComparison.CurrentCultureIgnoreCase;

        private static readonly Currency[] KnownCurrencies =
        {
            new RussianRuble(),
            new USDollar(),
            new Euro(),
            new TurkishLira(),
        };

        private readonly CultureInfo culture;
        private readonly char[] numericChars;

        protected Currency(CultureInfo culture)
        {
            this.culture = culture;
            this.numericChars =
                String.Concat(
                    $"{NormalSpaceChar}", // inculding space characters too
                    SpaceChars, // unusual space characters
                    this.culture.NumberFormat.PositiveSign,
                    this.culture.NumberFormat.NegativeSign,
                    this.culture.NumberFormat.NumberDecimalSeparator,
                    this.culture.NumberFormat.NumberGroupSeparator,
                    this.culture.NumberFormat.CurrencyDecimalSeparator,
                    this.culture.NumberFormat.CurrencyGroupSeparator,
                    String.Concat(this.culture.NumberFormat.NativeDigits))
                .Distinct()
                .OrderBy(ch => ch)
                .ToArray();
        }

        public static Currency Default => KnownCurrencies[0];

        public abstract string Name { get; }

        public abstract string Code { get; }

        public virtual string Symbol => this.culture.NumberFormat.CurrencySymbol;

        public virtual string WritingSymbol => this.Symbol;

        public abstract CurrencySymbolPlacement SymbolPlacement { get; }

        public abstract IReadOnlyCollection<string> MainUnitShortForms { get; }

        public abstract IReadOnlyCollection<string> FractionalUnitShortForms { get; }

        public override string ToString() => this.Code;

        public static Currency FromProvider(IFormatProvider? provider)
        {
            if (provider is null)
                return Default;

            foreach (var currency in KnownCurrencies)
            {
                if (currency.culture == provider || currency.culture.NumberFormat == provider)
                    return currency;
            }

            return Default;
        }

        public static Currency FromCode(string code)
        {
            if (String.IsNullOrWhiteSpace(code))
                return Default;

            foreach (var currency in KnownCurrencies)
            {
                if (String.Equals(currency.Code, code, CodeComparison))
                    return currency;
            }

            return Default;
        }

        public static Money ParseMoneyExact(ReadOnlySpan<char> writing)
        {
            if (TryParseMoneyExact(writing, out var money))
                return money;

            throw new FormatException();
        }

        public static bool TryParseMoneyExact(ReadOnlySpan<char> writing, out Money money)
        {
            foreach (var currency in KnownCurrencies)
            {
                if (currency.TryParseMoney(writing, out money))
                    return true;
            }

            money = default;
            return false;
        }

        public static Money GetMoney(decimal sum, ReadOnlySpan<char> unit = default)
        {
            var foundCurrency = KnownCurrencies[0];

            if (!unit.IsEmpty)
            {
                unit = unit.Trim('.');
                foreach (var currency in KnownCurrencies)
                {
                    if (Contains(currency.MainUnitShortForms, unit) || Contains(currency.FractionalUnitShortForms, unit))
                    {
                        foundCurrency = currency;
                        break;
                    }
                }
            }

            return new Money(foundCurrency.Convert(sum, unit), foundCurrency);
        }

        public Money ParseMoney(ReadOnlySpan<char> writing)
        {
            if (TryParseMoney(writing, out var money))
                return money;

            throw new FormatException();
        }

        public bool TryParseMoney(ReadOnlySpan<char> writing, out Money money)
        {
            var sum = default(NotationSpan);
            var unit = default(NotationSpan);
            money = default(Money);

            foreach (var note in Denote(writing))
            {
                if (sum.Category != NotationCategory.None && unit.Category != NotationCategory.None)
                {
                    if (TryParseSum(sum, out var value))
                    {
                        var newShare = GetMoney(value, unit);
                        if (newShare != default && newShare.Currency != (money.Currency ?? this))
                            return false;

                        money += newShare;
                    }
                    sum = default;
                    unit = default;
                }

                if (note.Category == NotationCategory.Sum)
                {
                    sum = note;
                }
                else
                {
                    unit = note;
                }
            }

            if (sum.Category == NotationCategory.None || !TryParseSum(sum, out var remainder))
            {
                return false;
            }

            var currency = money.Currency ?? this;
            if (unit.Category != NotationCategory.None)
            {
                var remainderMoney = GetMoney(remainder, unit);
                if (remainderMoney != default && remainderMoney.Currency != currency)
                    return false;

                money += remainderMoney;
            }
            else
            {
                if (money != default)
                {
                    remainder = currency.Convert(remainder, currency.FractionalUnitShortForms.First());
                }
                money += new Money(remainder, currency);
            }

            return true;
        }

        public override bool Equals(object? obj)
        {
            return obj is Currency other && Equals(other);
        }

        public bool Equals(Currency? other)
        {
            if (other is null)
                return false;
            if (Object.ReferenceEquals(this, other))
                return true;

            return
                String.Equals(this.Name, other.Name, WritingComparison) &&
                String.Equals(this.Code, other.Code, CodeComparison) &&
                String.Equals(this.Symbol, other.Symbol, WritingComparison);
        }

        public override int GetHashCode() => HashCode.Combine(this.Name, this.Code, this.Symbol);

        public int CompareTo(Currency? other)
        {
            if (other is null)
                return 1;

            return Array.IndexOf(KnownCurrencies, this).CompareTo(Array.IndexOf(KnownCurrencies, other));
        }

        public string ToString(decimal sum)
        {
            var hasFraction = Math.Abs(sum % 1m) != 0m;
            var sumWriting = sum.ToString(hasFraction ? "N2" : "N0", this.culture);

            if (this.SymbolPlacement == CurrencySymbolPlacement.BeforeSum)
                return this.WritingSymbol + sumWriting;

            return sumWriting + this.WritingSymbol;
        }

        public bool TryFormat(decimal sum, Span<char> destination, out int charsWritten)
        {
            charsWritten = 0;

            if (this.SymbolPlacement == CurrencySymbolPlacement.BeforeSum)
            {
                if (this.WritingSymbol.TryCopyTo(destination))
                {
                    charsWritten += this.WritingSymbol.Length;
                }
                else
                {
                    return false;
                }
            }

            var hasFraction = Math.Abs(sum % 1m) != 0m;
            if (sum.TryFormat(destination[charsWritten..], out var sumCharsWritten, hasFraction ? "N2" : "N0", this.culture))
            {
                charsWritten += sumCharsWritten;
            }
            else
            {
                return false;
            }

            if (this.SymbolPlacement == CurrencySymbolPlacement.AfterSum)
            {
                if (this.WritingSymbol.TryCopyTo(destination[charsWritten..]))
                {
                    charsWritten += this.WritingSymbol.Length;
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        protected decimal Convert(decimal sum, ReadOnlySpan<char> unit)
        {
            if (!unit.IsEmpty && Contains(this.FractionalUnitShortForms, unit))
                return sum / 100m;

            return sum;
        }

        private static bool Contains(IEnumerable<string> strs, ReadOnlySpan<char> span)
        {
            foreach (var str in strs)
            {
                if (span.Equals(str, WritingComparison))
                    return true;
            }

            return false;
        }

        private NotationEnumerator Denote(ReadOnlySpan<char> writing)
        {
            return new NotationEnumerator(this, writing);
        }

        private bool TryParseSum(ReadOnlySpan<char> span, out decimal sum)
        {
            if (Decimal.TryParse(span, NumberStyles.Any, this.culture, out sum))
                return true;

            if (span.IndexOfAny(SpaceChars) >= 0)
            {
                Span<char> normSpan = stackalloc char[span.Length];
                for (var i = 0; i != normSpan.Length; ++i)
                {
                    normSpan[i] = SpaceChars.IndexOf(span[i]) >= 0 ? NormalSpaceChar : span[i];
                }
                return Decimal.TryParse(normSpan, NumberStyles.Any, this.culture, out sum);
            }

            return false;
        }
    }

    public sealed class RussianRuble : Currency
    {
        private const string RubName = "Russian ruble";
        private const string RubCode = "RUB";
        private const string RubSymbol = "\u20bd";
        private const string RubWritingSymbol = " руб.";

        private static readonly IReadOnlyCollection<string> mainUnitShortForms = new[]
        {
            "РУБ", "Р", RubSymbol, "РР", RubCode
        };

        private static readonly IReadOnlyCollection<string> fractionalUnitShortForms = new[]
        {
            "КОП", "К"
        };

        public RussianRuble() : base(CultureInfo.GetCultureInfo("ru-RU"))
        {
        }

        public override string Name => RubName;
        public override string Code => RubCode;
        public override string Symbol => RubSymbol;
        public override string WritingSymbol => RubWritingSymbol;
        public override CurrencySymbolPlacement SymbolPlacement => CurrencySymbolPlacement.AfterSum;
        public override IReadOnlyCollection<string> MainUnitShortForms => mainUnitShortForms;
        public override IReadOnlyCollection<string> FractionalUnitShortForms => fractionalUnitShortForms;
    }

    public sealed class USDollar : Currency
    {
        private const string UsdName = "United States dollar";
        private const string UsdCode = "USD";
        private const string UsdSymbol = "$";
        private const string UsdFractionalSymbol = "\u00a2";

        private static readonly IReadOnlyCollection<string> mainUnitShortForms = new[]
        {
            UsdSymbol, "D", "DO", UsdCode, "DOL", "DOLLAR", "DOLLARS",
        };

        private static readonly IReadOnlyCollection<string> fractionalUnitShortForms = new[]
        {
            UsdFractionalSymbol, "C", "CC", "CT", "CENT", "CENTS",
        };

        public USDollar() : base(CultureInfo.GetCultureInfo("en-US"))
        {
        }

        public override string Name => UsdName;
        public override string Code => UsdCode;
        public override string Symbol => UsdSymbol;
        public override CurrencySymbolPlacement SymbolPlacement => CurrencySymbolPlacement.BeforeSum;
        public string FractionalSymbol => UsdFractionalSymbol;
        public CurrencySymbolPlacement FractionalSymbolPlacement => CurrencySymbolPlacement.AfterSum;
        public override IReadOnlyCollection<string> MainUnitShortForms => mainUnitShortForms;
        public override IReadOnlyCollection<string> FractionalUnitShortForms => fractionalUnitShortForms;
    }

    public sealed class Euro : Currency
    {
        private const string EurName = "Euro";
        private const string EurCode = "EUR";
        private const string EurSymbol = "\u20ac";
        private const string EurFractionalSymbol = "\u00a2";

        private static readonly IReadOnlyCollection<string> mainUnitShortForms = new[]
        {
            EurSymbol, "E", EurCode, "EURO", "EUROS",
        };

        private static readonly IReadOnlyCollection<string> fractionalUnitShortForms = new[]
        {
            EurFractionalSymbol, "C", "CC", "CT", "CENT", "CENTS",
        };

        public Euro() : base(CultureInfo.GetCultureInfo("fr-FR"))
        {
        }

        public override string Name => EurName;
        public override string Code => EurCode;
        public override string Symbol => EurSymbol;
        public override CurrencySymbolPlacement SymbolPlacement => CurrencySymbolPlacement.BeforeSum;
        public string FractionalSymbol => EurFractionalSymbol;
        public CurrencySymbolPlacement FractionalSymbolPlacement => CurrencySymbolPlacement.AfterSum;
        public override IReadOnlyCollection<string> MainUnitShortForms => mainUnitShortForms;
        public override IReadOnlyCollection<string> FractionalUnitShortForms => fractionalUnitShortForms;
    }

    public sealed class TurkishLira : Currency
    {
        private const string TryName = "Turkish lira";
        private const string TryCode = "TRY";
        private const string TrySymbol = "\u20ba";

        private static readonly IReadOnlyCollection<string> mainUnitShortForms = new[]
        {
            TrySymbol, "TL", "L",
        };

        private static readonly IReadOnlyCollection<string> fractionalUnitShortForms = new[]
        {
            "KR", "K", "KURUŞ",
        };

        public TurkishLira() 
            : base(CultureInfo.GetCultureInfo("tr-TR")) { }

        public override string Name => TryName;
        public override string Code => TryCode;
        public override CurrencySymbolPlacement SymbolPlacement => CurrencySymbolPlacement.BeforeSum;
        public CurrencySymbolPlacement FractionalSymbolPlacement => CurrencySymbolPlacement.AfterSum;
        public override IReadOnlyCollection<string> MainUnitShortForms => mainUnitShortForms;
        public override IReadOnlyCollection<string> FractionalUnitShortForms => fractionalUnitShortForms;
    }

    //todo: find alternatives in BCL
    static class CharSpanExtensions
    {
        public static int ClampStart(this ReadOnlySpan<char> span, int start, ReadOnlySpan<char> trimChars)
        {
            for (; start < span.Length; start++)
            {
                for (int i = 0; i < trimChars.Length; i++)
                {
                    if (span[start] == trimChars[i])
                    {
                        goto Next;
                    }
                }

                break;
            Next:
                ;
            }

            return start;
        }

        public static int ClampStartUntil(this ReadOnlySpan<char> span, int start, ReadOnlySpan<char> stopChars)
        {
            for (; start < span.Length; ++start)
            {
                for (var i = 0; i < stopChars.Length; ++i)
                {
                    if (span[start] == stopChars[i])
                    {
                        return start;
                    }
                }
            }

            return start;
        }
    }
}