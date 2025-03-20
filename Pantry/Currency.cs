namespace Pantry
{
    using System;
    using System.Buffers;
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
            private NotationSpan current;

            public NotationEnumerator(Currency currency, ReadOnlySpan<char> writing)
            {
                this.currency = currency;
                this.writing = writing.Trim();
                this.current = default;
            }

            public readonly NotationSpan Current => this.current;

            public readonly NotationEnumerator GetEnumerator() => this;

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

                this.current = new NotationSpan(this.writing[..endPos], category);
                this.writing = this.writing[endPos..];

                return true;
            }
        }

        private const char NormalSpaceChar = '\x20';
        private const string SpaceChars = "\t\xA0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200A\u202F\u205F\u3000";
        private const StringComparison CodeComparison = StringComparison.OrdinalIgnoreCase;
        private const StringComparison WritingComparison = StringComparison.CurrentCultureIgnoreCase;

        internal static readonly Currency[] KnownCurrencies =
        [
            new RussianRuble(),
            new USDollar(),
            new Euro(),
            new TurkishLira(),
            new UAEDirham(),
            new ArmenianDram(),
            new AustralianDollar(),
            new AzerbaijaniManat(),
            new BulgarianLev(),
            new BrazilianReal(),
            new BelarusianRuble(),
            new CanadianDollar(),
            new SwissFranc(),
            new ChineseYuan(),
            new CzechKoruna(),
            new DanishKrone(),
            new EgyptianPound(),
            new BritishPound(),
            new GeorgianLari(),
            new HongKongDollar(),
            new HungarianForint(),
            new IndonesianRupiah(),
            new IsraeliNewShekel(),
            new IndianRupee(),
            new JapaneseYen(),
            new KyrgyzstaniSom(),
            new SouthKoreanWon(),
            new KazakhstaniTenge(),
            new MoldovanLeu(),
            new MexicanPeso(),
            new NorwegianKrone(),
            new NewZealandDollar(),
            new PolishZloty(),
            new QatariRiyal(),
            new RomanianLeu(),
            new SerbianDinar(),
            new SwedishKrona(),
            new SingaporeDollar(),
            new ThaiBaht(),
            new TajikistaniSomoni(),
            new TurkmenistanManat(),
            new UkrainianHryvnia(),
            new UzbekistanSom(),
            new VietnameseDong(),
            new SpecialDrawingRights(),
            new SouthAfricanRand(),
        ];

        private readonly CultureInfo culture;
        private readonly SearchValues<char> numericChars;

        protected Currency(CultureInfo culture)
        {
            this.culture = culture;
            this.numericChars = SearchValues.Create(
                string.Concat(
                    NormalSpaceChar.ToString(), // inculding space characters too
                    SpaceChars, // unusual space characters
                    this.culture.NumberFormat.PositiveSign,
                    this.culture.NumberFormat.NegativeSign,
                    this.culture.NumberFormat.NumberDecimalSeparator,
                    this.culture.NumberFormat.NumberGroupSeparator,
                    this.culture.NumberFormat.CurrencyDecimalSeparator,
                    this.culture.NumberFormat.CurrencyGroupSeparator,
                    string.Concat(this.culture.NumberFormat.NativeDigits))
                .Order()
                .Distinct()
                .ToArray());
        }

        public static Currency Default => KnownCurrencies[0];

        public abstract string Name { get; }

        public abstract string Code { get; }

        public virtual string Symbol => this.culture.NumberFormat.CurrencySymbol;

        public virtual string WritingSymbol => this.Symbol;

        public abstract CurrencySymbolPlacement SymbolPlacement { get; }

        public abstract IReadOnlyCollection<string> UnitShortForms { get; }

        public abstract IReadOnlyCollection<string> SubunitShortForms { get; }

        internal CultureInfo Culture => this.culture;

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
                    if (Contains(currency.UnitShortForms, unit) || Contains(currency.SubunitShortForms, unit))
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
                    remainder = currency.Convert(remainder, currency.SubunitShortForms.First());
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
            if (!unit.IsEmpty && Contains(this.SubunitShortForms, unit))
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

    public sealed class UAEDirham : Currency
    {
        private const string AedName = "United Arab Emirates dirham";
        private const string AedCode = "AED";

        private static readonly IReadOnlyCollection<string> mainUnitShortForms = new[]
        {
            "\u062f\u002e\u0625\u002e\u200f", "\u062f\u002e\u0625\u002e", "\u062f\u002e\u0625", "DH", "DHS", AedCode
        };

        private static readonly IReadOnlyCollection<string> fractionalUnitShortForms = new[]
        {
            "FILS", "fulūs", "fulus", "\u0641\u0644\u0633",
        };

        public UAEDirham() : base(CultureInfo.GetCultureInfo("ar-AE"))
        {
        }

        public override string Name => AedName;
        public override string Code => AedCode;
        public override CurrencySymbolPlacement SymbolPlacement => CurrencySymbolPlacement.AfterSum;
        public override IReadOnlyCollection<string> UnitShortForms => mainUnitShortForms;
        public override IReadOnlyCollection<string> SubunitShortForms => fractionalUnitShortForms;
    }

    public sealed class ArmenianDram : Currency
    {
        private const string AmdName = "Armenian dram";
        private const string AmdCode = "AMD";
        private const string AmdSymbol = "֏";

        private static readonly IReadOnlyCollection<string> mainUnitShortForms = new[]
        {
            AmdSymbol, AmdCode
        };

        private static readonly IReadOnlyCollection<string> fractionalUnitShortForms = new[]
        {
            "LUMA", "լումա"
        };

        public ArmenianDram() : base(CultureInfo.GetCultureInfo("hy-AM"))
        {
        }

        public override string Name => AmdName;
        public override string Code => AmdCode;
        public override string Symbol => AmdSymbol;
        public override CurrencySymbolPlacement SymbolPlacement => CurrencySymbolPlacement.BeforeSum;
        public override IReadOnlyCollection<string> UnitShortForms => mainUnitShortForms;
        public override IReadOnlyCollection<string> SubunitShortForms => fractionalUnitShortForms;
    }

    public sealed class AustralianDollar : Currency
    {
        private const string AudName = "Australian dollar";
        private const string AudCode = "AUD";
        private const string AudSymbol = "$";

        private static readonly IReadOnlyCollection<string> mainUnitShortForms = new[]
        {
            AudSymbol, "A$", "AU$", AudCode, "DOLLAR", "DOLLARS"
        };

        private static readonly IReadOnlyCollection<string> fractionalUnitShortForms = new[]
        {
            "CENT", "CENTS"
        };

        public AustralianDollar() : base(CultureInfo.GetCultureInfo("en-AU"))
        {
        }

        public override string Name => AudName;
        public override string Code => AudCode;
        public override string Symbol => AudSymbol;
        public override CurrencySymbolPlacement SymbolPlacement => CurrencySymbolPlacement.BeforeSum;
        public override IReadOnlyCollection<string> UnitShortForms => mainUnitShortForms;
        public override IReadOnlyCollection<string> SubunitShortForms => fractionalUnitShortForms;
    }

    public sealed class AzerbaijaniManat : Currency
    {
        private const string AznName = "Azerbaijani manat";
        private const string AznCode = "AZN";
        private const string AznSymbol = "₼";

        private static readonly IReadOnlyCollection<string> mainUnitShortForms = new[]
        {
            AznSymbol, AznCode
        };

        private static readonly IReadOnlyCollection<string> fractionalUnitShortForms = new[]
        {
            "gapik", "gapiks", "QƏPİK", "QEPIC"
        };

        public AzerbaijaniManat() : base(CultureInfo.GetCultureInfo("az-AZ"))
        {
        }

        public override string Name => AznName;
        public override string Code => AznCode;
        public override string Symbol => AznSymbol;
        public override CurrencySymbolPlacement SymbolPlacement => CurrencySymbolPlacement.BeforeSum;
        public override IReadOnlyCollection<string> UnitShortForms => mainUnitShortForms;
        public override IReadOnlyCollection<string> SubunitShortForms => fractionalUnitShortForms;
    }

    public sealed class BulgarianLev : Currency
    {
        private const string BgnName = "Bulgarian lev";
        private const string BgnCode = "BGN";
        private const string BgnSymbol = "лв";

        private static readonly IReadOnlyCollection<string> mainUnitShortForms = new[]
        {
            BgnSymbol, BgnCode, "lev", "leva"
        };

        private static readonly IReadOnlyCollection<string> fractionalUnitShortForms = new[]
        {
            "СТОТИНКА", "STOTINKA", "stotinki"
        };

        public BulgarianLev() : base(CultureInfo.GetCultureInfo("bg-BG"))
        {
        }

        public override string Name => BgnName;
        public override string Code => BgnCode;
        public override string Symbol => BgnSymbol;
        public override CurrencySymbolPlacement SymbolPlacement => CurrencySymbolPlacement.AfterSum;
        public override IReadOnlyCollection<string> UnitShortForms => mainUnitShortForms;
        public override IReadOnlyCollection<string> SubunitShortForms => fractionalUnitShortForms;
    }

    public sealed class BrazilianReal : Currency
    {
        private const string BrlName = "Brazilian real";
        private const string BrlCode = "BRL";
        private const string BrlSymbol = "R$";

        private static readonly IReadOnlyCollection<string> mainUnitShortForms = new[]
        {
            BrlSymbol, BrlCode
        };

        private static readonly IReadOnlyCollection<string> fractionalUnitShortForms = new[]
        {
            "CENTAVO", "CENTAVOS"
        };

        public BrazilianReal() : base(CultureInfo.GetCultureInfo("pt-BR"))
        {
        }

        public override string Name => BrlName;
        public override string Code => BrlCode;
        public override string Symbol => BrlSymbol;
        public override CurrencySymbolPlacement SymbolPlacement => CurrencySymbolPlacement.BeforeSum;
        public override IReadOnlyCollection<string> UnitShortForms => mainUnitShortForms;
        public override IReadOnlyCollection<string> SubunitShortForms => fractionalUnitShortForms;
    }

    public sealed class BelarusianRuble : Currency
    {
        private const string BynName = "Belarusian ruble";
        private const string BynCode = "BYN";
        private const string BynSymbol = "Br";

        private static readonly IReadOnlyCollection<string> mainUnitShortForms = new[]
        {
            BynSymbol, BynCode
        };

        private static readonly IReadOnlyCollection<string> fractionalUnitShortForms = new[]
        {
            "KAPEIKA", "KAPEYKA"
        };

        public BelarusianRuble() : base(CultureInfo.GetCultureInfo("be-BY"))
        {
        }

        public override string Name => BynName;
        public override string Code => BynCode;
        public override string Symbol => BynSymbol;
        public override CurrencySymbolPlacement SymbolPlacement => CurrencySymbolPlacement.AfterSum;
        public override IReadOnlyCollection<string> UnitShortForms => mainUnitShortForms;
        public override IReadOnlyCollection<string> SubunitShortForms => fractionalUnitShortForms;
    }

    public sealed class CanadianDollar : Currency
    {
        private const string CadName = "Canadian dollar";
        private const string CadCode = "CAD";
        private const string CadSymbol = "$";

        private static readonly IReadOnlyCollection<string> mainUnitShortForms = new[]
        {
            CadSymbol, "CA$", CadCode, "DOLLAR", "DOLLARS"
        };

        private static readonly IReadOnlyCollection<string> fractionalUnitShortForms = new[]
        {
            "CENT", "CENTS"
        };

        public CanadianDollar() : base(CultureInfo.GetCultureInfo("en-CA"))
        {
        }

        public override string Name => CadName;
        public override string Code => CadCode;
        public override string Symbol => CadSymbol;
        public override CurrencySymbolPlacement SymbolPlacement => CurrencySymbolPlacement.BeforeSum;
        public override IReadOnlyCollection<string> UnitShortForms => mainUnitShortForms;
        public override IReadOnlyCollection<string> SubunitShortForms => fractionalUnitShortForms;
    }

    public sealed class SwissFranc : Currency
    {
        private const string ChfName = "Swiss franc";
        private const string ChfCode = "CHF";
        private const string ChfSymbol = "CHF";

        private static readonly IReadOnlyCollection<string> mainUnitShortForms = new[]
        {
            ChfSymbol, "Fr.", "SFr."
        };

        private static readonly IReadOnlyCollection<string> fractionalUnitShortForms = new[]
        {
            "RAP", "CENTIME", "CENTIMES", "RP."
        };

        public SwissFranc() : base(CultureInfo.GetCultureInfo("de-CH"))
        {
        }

        public override string Name => ChfName;
        public override string Code => ChfCode;
        public override string Symbol => ChfSymbol;
        public override CurrencySymbolPlacement SymbolPlacement => CurrencySymbolPlacement.BeforeSum;
        public override IReadOnlyCollection<string> UnitShortForms => mainUnitShortForms;
        public override IReadOnlyCollection<string> SubunitShortForms => fractionalUnitShortForms;
    }

    public sealed class ChineseYuan : Currency
    {
        private const string CnyName = "Chinese yuan";
        private const string CnyCode = "CNY";
        private const string CnySymbol = "¥";

        private static readonly IReadOnlyCollection<string> mainUnitShortForms = new[]
        {
            CnySymbol, "RMB", CnyCode
        };

        private static readonly IReadOnlyCollection<string> fractionalUnitShortForms = new[]
        {
            "JIAO", "FEN"
        };

        public ChineseYuan() : base(CultureInfo.GetCultureInfo("zh-CN"))
        {
        }

        public override string Name => CnyName;
        public override string Code => CnyCode;
        public override string Symbol => CnySymbol;
        public override CurrencySymbolPlacement SymbolPlacement => CurrencySymbolPlacement.BeforeSum;
        public override IReadOnlyCollection<string> UnitShortForms => mainUnitShortForms;
        public override IReadOnlyCollection<string> SubunitShortForms => fractionalUnitShortForms;
    }

    public sealed class CzechKoruna : Currency
    {
        private const string CzkName = "Czech koruna";
        private const string CzkCode = "CZK";
        private const string CzkSymbol = "Kč";

        private static readonly IReadOnlyCollection<string> mainUnitShortForms = new[]
        {
            CzkSymbol, CzkCode
        };

        private static readonly IReadOnlyCollection<string> fractionalUnitShortForms = new[]
        {
            "HALÉŘ", "HALER"
        };

        public CzechKoruna() : base(CultureInfo.GetCultureInfo("cs-CZ"))
        {
        }

        public override string Name => CzkName;
        public override string Code => CzkCode;
        public override string Symbol => CzkSymbol;
        public override CurrencySymbolPlacement SymbolPlacement => CurrencySymbolPlacement.AfterSum;
        public override IReadOnlyCollection<string> UnitShortForms => mainUnitShortForms;
        public override IReadOnlyCollection<string> SubunitShortForms => fractionalUnitShortForms;
    }

    public sealed class DanishKrone : Currency
    {
        private const string DkkName = "Danish krone";
        private const string DkkCode = "DKK";
        private const string DkkSymbol = "kr";

        private static readonly IReadOnlyCollection<string> mainUnitShortForms = new[]
        {
            DkkSymbol, DkkCode
        };

        private static readonly IReadOnlyCollection<string> fractionalUnitShortForms = new[]
        {
            "ØRE", "ORE"
        };

        public DanishKrone() : base(CultureInfo.GetCultureInfo("da-DK"))
        {
        }

        public override string Name => DkkName;
        public override string Code => DkkCode;
        public override string Symbol => DkkSymbol;
        public override CurrencySymbolPlacement SymbolPlacement => CurrencySymbolPlacement.AfterSum;
        public override IReadOnlyCollection<string> UnitShortForms => mainUnitShortForms;
        public override IReadOnlyCollection<string> SubunitShortForms => fractionalUnitShortForms;
    }

    public sealed class EgyptianPound : Currency
    {
        private const string EgpName = "Egyptian pound";
        private const string EgpCode = "EGP";

        private static readonly IReadOnlyCollection<string> mainUnitShortForms = new[]
        {
            "\u062c\u002e\u0645\u002e\u200f", "\u062c\u002e\u0645\u002e", "\u062c\u002e\u0645", "LE", "£", "E£", "£E", EgpCode
        };

        private static readonly IReadOnlyCollection<string> fractionalUnitShortForms = new[]
        {
            "PT", "PIASTRE", "PIASTRES", "piaster"
        };

        public EgyptianPound() : base(CultureInfo.GetCultureInfo("ar-EG"))
        {
        }

        public override string Name => EgpName;
        public override string Code => EgpCode;
        public override CurrencySymbolPlacement SymbolPlacement => CurrencySymbolPlacement.BeforeSum;
        public override IReadOnlyCollection<string> UnitShortForms => mainUnitShortForms;
        public override IReadOnlyCollection<string> SubunitShortForms => fractionalUnitShortForms;
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
        public override IReadOnlyCollection<string> UnitShortForms => mainUnitShortForms;
        public override IReadOnlyCollection<string> SubunitShortForms => fractionalUnitShortForms;
    }

    public sealed class BritishPound : Currency
    {
        private const string GbpName = "British pound";
        private const string GbpCode = "GBP";
        private const string GbpSymbol = "£";

        private static readonly IReadOnlyCollection<string> mainUnitShortForms = new[]
        {
            GbpSymbol, GbpCode, "POUND", "POUNDS"
        };

        private static readonly IReadOnlyCollection<string> fractionalUnitShortForms = new[]
        {
            "PENCE", "PENNY"
        };

        public BritishPound() : base(CultureInfo.GetCultureInfo("en-GB"))
        {
        }

        public override string Name => GbpName;
        public override string Code => GbpCode;
        public override string Symbol => GbpSymbol;
        public override CurrencySymbolPlacement SymbolPlacement => CurrencySymbolPlacement.BeforeSum;
        public override IReadOnlyCollection<string> UnitShortForms => mainUnitShortForms;
        public override IReadOnlyCollection<string> SubunitShortForms => fractionalUnitShortForms;
    }

    public sealed class GeorgianLari : Currency
    {
        private const string GelName = "Georgian lari";
        private const string GelCode = "GEL";
        private const string GelSymbol = "₾";

        private static readonly IReadOnlyCollection<string> mainUnitShortForms = new[]
        {
            GelSymbol, GelCode
        };

        private static readonly IReadOnlyCollection<string> fractionalUnitShortForms = new[]
        {
            "TETRI"
        };

        public GeorgianLari() : base(CultureInfo.GetCultureInfo("ka-GE"))
        {
        }

        public override string Name => GelName;
        public override string Code => GelCode;
        public override string Symbol => GelSymbol;
        public override CurrencySymbolPlacement SymbolPlacement => CurrencySymbolPlacement.AfterSum;
        public override IReadOnlyCollection<string> UnitShortForms => mainUnitShortForms;
        public override IReadOnlyCollection<string> SubunitShortForms => fractionalUnitShortForms;
    }

    public sealed class HongKongDollar : Currency
    {
        private const string HkdName = "Hong Kong dollar";
        private const string HkdCode = "HKD";
        private const string HkdSymbol = "HK$";

        private static readonly IReadOnlyCollection<string> mainUnitShortForms = new[]
        {
            HkdSymbol, HkdCode, "DOLLAR", "DOLLARS"
        };

        private static readonly IReadOnlyCollection<string> fractionalUnitShortForms = new[]
        {
            "CENT", "CENTS"
        };

        public HongKongDollar() : base(CultureInfo.GetCultureInfo("zh-HK"))
        {
        }

        public override string Name => HkdName;
        public override string Code => HkdCode;
        public override string Symbol => HkdSymbol;
        public override CurrencySymbolPlacement SymbolPlacement => CurrencySymbolPlacement.BeforeSum;
        public override IReadOnlyCollection<string> UnitShortForms => mainUnitShortForms;
        public override IReadOnlyCollection<string> SubunitShortForms => fractionalUnitShortForms;
    }

    public sealed class HungarianForint : Currency
    {
        private const string HufName = "Hungarian forint";
        private const string HufCode = "HUF";
        private const string HufSymbol = "Ft";

        private static readonly IReadOnlyCollection<string> mainUnitShortForms = new[]
        {
            HufSymbol, HufCode
        };

        private static readonly IReadOnlyCollection<string> fractionalUnitShortForms = new[]
        {
            "FILLER"
        };

        public HungarianForint() : base(CultureInfo.GetCultureInfo("hu-HU"))
        {
        }

        public override string Name => HufName;
        public override string Code => HufCode;
        public override string Symbol => HufSymbol;
        public override CurrencySymbolPlacement SymbolPlacement => CurrencySymbolPlacement.AfterSum;
        public override IReadOnlyCollection<string> UnitShortForms => mainUnitShortForms;
        public override IReadOnlyCollection<string> SubunitShortForms => fractionalUnitShortForms;
    }

    public sealed class IndonesianRupiah : Currency
    {
        private const string IdrName = "Indonesian rupiah";
        private const string IdrCode = "IDR";
        private const string IdrSymbol = "Rp";

        private static readonly IReadOnlyCollection<string> mainUnitShortForms = new[]
        {
            IdrSymbol, IdrCode
        };

        private static readonly IReadOnlyCollection<string> fractionalUnitShortForms = new[]
        {
            "SEN"
        };

        public IndonesianRupiah() : base(CultureInfo.GetCultureInfo("id-ID"))
        {
        }

        public override string Name => IdrName;
        public override string Code => IdrCode;
        public override string Symbol => IdrSymbol;
        public override CurrencySymbolPlacement SymbolPlacement => CurrencySymbolPlacement.BeforeSum;
        public override IReadOnlyCollection<string> UnitShortForms => mainUnitShortForms;
        public override IReadOnlyCollection<string> SubunitShortForms => fractionalUnitShortForms;
    }

    public sealed class IsraeliNewShekel : Currency
    {
        private const string IlsName = "Israeli new shekel";
        private const string IlsCode = "ILS";
        private const string IlsSymbol = "₪";

        private static readonly IReadOnlyCollection<string> mainUnitShortForms = new[]
        {
            IlsSymbol, IlsCode, "NIS"
        };

        private static readonly IReadOnlyCollection<string> fractionalUnitShortForms = new[]
        {
            "AGOROT", "AGORA"
        };

        public IsraeliNewShekel() : base(CultureInfo.GetCultureInfo("he-IL"))
        {
        }

        public override string Name => IlsName;
        public override string Code => IlsCode;
        public override string Symbol => IlsSymbol;
        public override CurrencySymbolPlacement SymbolPlacement => CurrencySymbolPlacement.BeforeSum;
        public override IReadOnlyCollection<string> UnitShortForms => mainUnitShortForms;
        public override IReadOnlyCollection<string> SubunitShortForms => fractionalUnitShortForms;
    }

    public sealed class IndianRupee : Currency
    {
        private const string InrName = "Indian rupee";
        private const string InrCode = "INR";
        private const string InrSymbol = "₹";

        private static readonly IReadOnlyCollection<string> mainUnitShortForms = new[]
        {
            InrSymbol, InrCode, "Re", "Rs"
        };

        private static readonly IReadOnlyCollection<string> fractionalUnitShortForms = new[]
        {
            "PAISE", "PAISA"
        };

        public IndianRupee() : base(CultureInfo.GetCultureInfo("en-IN"))
        {
        }

        public override string Name => InrName;
        public override string Code => InrCode;
        public override string Symbol => InrSymbol;
        public override CurrencySymbolPlacement SymbolPlacement => CurrencySymbolPlacement.BeforeSum;
        public override IReadOnlyCollection<string> UnitShortForms => mainUnitShortForms;
        public override IReadOnlyCollection<string> SubunitShortForms => fractionalUnitShortForms;
    }

    public sealed class JapaneseYen : Currency
    {
        private const string JpyName = "Japanese yen";
        private const string JpyCode = "JPY";
        private const string JpySymbol = "￥";

        private static readonly IReadOnlyCollection<string> mainUnitShortForms = new[]
        {
            JpySymbol, JpyCode
        };

        private static readonly IReadOnlyCollection<string> fractionalUnitShortForms = new[]
        {
            "SEN"
        };

        public JapaneseYen() : base(CultureInfo.GetCultureInfo("ja-JP"))
        {
        }

        public override string Name => JpyName;
        public override string Code => JpyCode;
        public override string Symbol => JpySymbol;
        public override CurrencySymbolPlacement SymbolPlacement => CurrencySymbolPlacement.BeforeSum;
        public override IReadOnlyCollection<string> UnitShortForms => mainUnitShortForms;
        public override IReadOnlyCollection<string> SubunitShortForms => fractionalUnitShortForms;
    }

    public sealed class KyrgyzstaniSom : Currency
    {
        private const string KgsName = "Kyrgyzstani som";
        private const string KgsCode = "KGS";
        private const string KgsSymbol = "сом";

        private static readonly IReadOnlyCollection<string> mainUnitShortForms = new[]
        {
            KgsSymbol, KgsCode
        };

        private static readonly IReadOnlyCollection<string> fractionalUnitShortForms = new[]
        {
            "TYIYN", "TYIN"
        };

        public KyrgyzstaniSom() : base(CultureInfo.GetCultureInfo("ky-KG"))
        {
        }

        public override string Name => KgsName;
        public override string Code => KgsCode;
        public override string Symbol => KgsSymbol;
        public override CurrencySymbolPlacement SymbolPlacement => CurrencySymbolPlacement.AfterSum;
        public override IReadOnlyCollection<string> UnitShortForms => mainUnitShortForms;
        public override IReadOnlyCollection<string> SubunitShortForms => fractionalUnitShortForms;
    }

    public sealed class SouthKoreanWon : Currency
    {
        private const string KrwName = "South Korean won";
        private const string KrwCode = "KRW";
        private const string KrwSymbol = "₩";

        private static readonly IReadOnlyCollection<string> mainUnitShortForms = new[]
        {
            KrwSymbol, KrwCode
        };

        private static readonly IReadOnlyCollection<string> fractionalUnitShortForms = new[]
        {
            "JEON"
        };

        public SouthKoreanWon() : base(CultureInfo.GetCultureInfo("ko-KR"))
        {
        }

        public override string Name => KrwName;
        public override string Code => KrwCode;
        public override string Symbol => KrwSymbol;
        public override CurrencySymbolPlacement SymbolPlacement => CurrencySymbolPlacement.BeforeSum;
        public override IReadOnlyCollection<string> UnitShortForms => mainUnitShortForms;
        public override IReadOnlyCollection<string> SubunitShortForms => fractionalUnitShortForms;
    }

    public sealed class KazakhstaniTenge : Currency
    {
        private const string KztName = "Kazakhstani tenge";
        private const string KztCode = "KZT";
        private const string KztSymbol = "₸";

        private static readonly IReadOnlyCollection<string> mainUnitShortForms = new[]
        {
            KztSymbol, KztCode
        };

        private static readonly IReadOnlyCollection<string> fractionalUnitShortForms = new[]
        {
            "TIYN"
        };

        public KazakhstaniTenge() : base(CultureInfo.GetCultureInfo("kk-KZ"))
        {
        }

        public override string Name => KztName;
        public override string Code => KztCode;
        public override string Symbol => KztSymbol;
        public override CurrencySymbolPlacement SymbolPlacement => CurrencySymbolPlacement.AfterSum;
        public override IReadOnlyCollection<string> UnitShortForms => mainUnitShortForms;
        public override IReadOnlyCollection<string> SubunitShortForms => fractionalUnitShortForms;
    }

    public sealed class MoldovanLeu : Currency
    {
        private const string MdlName = "Moldovan leu";
        private const string MdlCode = "MDL";
        private const string MdlSymbol = "L";

        private static readonly IReadOnlyCollection<string> mainUnitShortForms = new[]
        {
            MdlSymbol, MdlCode
        };

        private static readonly IReadOnlyCollection<string> fractionalUnitShortForms = new[]
        {
            "BAN", "BANI"
        };

        public MoldovanLeu() : base(CultureInfo.GetCultureInfo("ro-MD"))
        {
        }

        public override string Name => MdlName;
        public override string Code => MdlCode;
        public override string Symbol => MdlSymbol;
        public override CurrencySymbolPlacement SymbolPlacement => CurrencySymbolPlacement.AfterSum;
        public override IReadOnlyCollection<string> UnitShortForms => mainUnitShortForms;
        public override IReadOnlyCollection<string> SubunitShortForms => fractionalUnitShortForms;
    }

    public sealed class MexicanPeso : Currency
    {
        private const string MxnName = "Mexican peso";
        private const string MxnCode = "MXN";
        private const string MxnSymbol = "$";

        private static readonly IReadOnlyCollection<string> mainUnitShortForms = new[]
        {
            MxnSymbol, MxnCode, "MX$"
        };

        private static readonly IReadOnlyCollection<string> fractionalUnitShortForms = new[]
        {
            "CENTAVO", "CENTAVOS"
        };

        public MexicanPeso() : base(CultureInfo.GetCultureInfo("es-MX"))
        {
        }

        public override string Name => MxnName;
        public override string Code => MxnCode;
        public override string Symbol => MxnSymbol;
        public override CurrencySymbolPlacement SymbolPlacement => CurrencySymbolPlacement.BeforeSum;
        public override IReadOnlyCollection<string> UnitShortForms => mainUnitShortForms;
        public override IReadOnlyCollection<string> SubunitShortForms => fractionalUnitShortForms;
    }

    public sealed class NorwegianKrone : Currency
    {
        private const string NokName = "Norwegian krone";
        private const string NokCode = "NOK";
        private const string NokSymbol = "kr";

        private static readonly IReadOnlyCollection<string> mainUnitShortForms = new[]
        {
            NokSymbol, NokCode, "kroner", "krone"
        };

        private static readonly IReadOnlyCollection<string> fractionalUnitShortForms = new[]
        {
            "ØRE", "ORE"
        };

        public NorwegianKrone() : base(CultureInfo.GetCultureInfo("nb-NO"))
        {
        }

        public override string Name => NokName;
        public override string Code => NokCode;
        public override string Symbol => NokSymbol;
        public override CurrencySymbolPlacement SymbolPlacement => CurrencySymbolPlacement.AfterSum;
        public override IReadOnlyCollection<string> UnitShortForms => mainUnitShortForms;
        public override IReadOnlyCollection<string> SubunitShortForms => fractionalUnitShortForms;
    }

    public sealed class NewZealandDollar : Currency
    {
        private const string NzdName = "New Zealand dollar";
        private const string NzdCode = "NZD";
        private const string NzdSymbol = "$";

        private static readonly IReadOnlyCollection<string> mainUnitShortForms = new[]
        {
            NzdSymbol, "NZ$", NzdCode, "DOLLAR", "DOLLARS"
        };

        private static readonly IReadOnlyCollection<string> fractionalUnitShortForms = new[]
        {
            "CENT", "CENTS"
        };

        public NewZealandDollar() : base(CultureInfo.GetCultureInfo("en-NZ"))
        {
        }

        public override string Name => NzdName;
        public override string Code => NzdCode;
        public override string Symbol => NzdSymbol;
        public override CurrencySymbolPlacement SymbolPlacement => CurrencySymbolPlacement.BeforeSum;
        public override IReadOnlyCollection<string> UnitShortForms => mainUnitShortForms;
        public override IReadOnlyCollection<string> SubunitShortForms => fractionalUnitShortForms;
    }

    public sealed class PolishZloty : Currency
    {
        private const string PlnName = "Polish złoty";
        private const string PlnCode = "PLN";
        private const string PlnSymbol = "zł";

        private static readonly IReadOnlyCollection<string> mainUnitShortForms = new[]
        {
            PlnSymbol, PlnCode
        };

        private static readonly IReadOnlyCollection<string> fractionalUnitShortForms = new[]
        {
            "GROSZ", "GR"
        };

        public PolishZloty() : base(CultureInfo.GetCultureInfo("pl-PL"))
        {
        }

        public override string Name => PlnName;
        public override string Code => PlnCode;
        public override string Symbol => PlnSymbol;
        public override CurrencySymbolPlacement SymbolPlacement => CurrencySymbolPlacement.AfterSum;
        public override IReadOnlyCollection<string> UnitShortForms => mainUnitShortForms;
        public override IReadOnlyCollection<string> SubunitShortForms => fractionalUnitShortForms;
    }

    public sealed class QatariRiyal : Currency
    {
        private const string QarName = "Qatari riyal";
        private const string QarCode = "QAR";

        private static readonly IReadOnlyCollection<string> mainUnitShortForms = new[]
        {
            "\ufdfc", "\u0631\u002e\u0642\u200e", QarCode, "QR"
        };

        private static readonly IReadOnlyCollection<string> fractionalUnitShortForms = new[]
        {
            "DIRHAM", "DERHAM"
        };

        public QatariRiyal() : base(CultureInfo.GetCultureInfo("ar-QA"))
        {
        }

        public override string Name => QarName;
        public override string Code => QarCode;
        public override CurrencySymbolPlacement SymbolPlacement => CurrencySymbolPlacement.BeforeSum;
        public override IReadOnlyCollection<string> UnitShortForms => mainUnitShortForms;
        public override IReadOnlyCollection<string> SubunitShortForms => fractionalUnitShortForms;
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
        public override IReadOnlyCollection<string> UnitShortForms => mainUnitShortForms;
        public override IReadOnlyCollection<string> SubunitShortForms => fractionalUnitShortForms;
    }

    public sealed class RomanianLeu : Currency
    {
        private const string RonName = "Romanian leu";
        private const string RonCode = "RON";
        private const string RonSymbol = "lei";

        private static readonly IReadOnlyCollection<string> mainUnitShortForms = new[]
        {
            RonSymbol, RonCode, "leu"
        };

        private static readonly IReadOnlyCollection<string> fractionalUnitShortForms = new[]
        {
            "BAN", "BANI"
        };

        public RomanianLeu() : base(CultureInfo.GetCultureInfo("ro-RO"))
        {
        }

        public override string Name => RonName;
        public override string Code => RonCode;
        public override string Symbol => RonSymbol;
        public override CurrencySymbolPlacement SymbolPlacement => CurrencySymbolPlacement.AfterSum;
        public override IReadOnlyCollection<string> UnitShortForms => mainUnitShortForms;
        public override IReadOnlyCollection<string> SubunitShortForms => fractionalUnitShortForms;
    }

    public sealed class SerbianDinar : Currency
    {
        private const string RsdName = "Serbian dinar";
        private const string RsdCode = "RSD";
        private const string RsdSymbol = "дин";

        private static readonly IReadOnlyCollection<string> mainUnitShortForms = new[]
        {
            RsdSymbol, RsdCode
        };

        private static readonly IReadOnlyCollection<string> fractionalUnitShortForms = new[]
        {
            "PARA"
        };

        public SerbianDinar() : base(CultureInfo.GetCultureInfo("sr-RS"))
        {
        }

        public override string Name => RsdName;
        public override string Code => RsdCode;
        public override string Symbol => RsdSymbol;
        public override CurrencySymbolPlacement SymbolPlacement => CurrencySymbolPlacement.AfterSum;
        public override IReadOnlyCollection<string> UnitShortForms => mainUnitShortForms;
        public override IReadOnlyCollection<string> SubunitShortForms => fractionalUnitShortForms;
    }

    public sealed class SwedishKrona : Currency
    {
        private const string SekName = "Swedish krona";
        private const string SekCode = "SEK";
        private const string SekSymbol = "kr";

        private static readonly IReadOnlyCollection<string> mainUnitShortForms = new[]
        {
            SekSymbol, SekCode
        };

        private static readonly IReadOnlyCollection<string> fractionalUnitShortForms = new[]
        {
            "ÖRE", "ORE"
        };

        public SwedishKrona() : base(CultureInfo.GetCultureInfo("sv-SE"))
        {
        }

        public override string Name => SekName;
        public override string Code => SekCode;
        public override string Symbol => SekSymbol;
        public override CurrencySymbolPlacement SymbolPlacement => CurrencySymbolPlacement.AfterSum;
        public override IReadOnlyCollection<string> UnitShortForms => mainUnitShortForms;
        public override IReadOnlyCollection<string> SubunitShortForms => fractionalUnitShortForms;
    }

    public sealed class SingaporeDollar : Currency
    {
        private const string SgdName = "Singapore dollar";
        private const string SgdCode = "SGD";
        private const string SgdSymbol = "$";

        private static readonly IReadOnlyCollection<string> mainUnitShortForms = new[]
        {
            SgdSymbol, "S$", SgdCode, "DOLLAR", "DOLLARS"
        };

        private static readonly IReadOnlyCollection<string> fractionalUnitShortForms = new[]
        {
            "CENT", "CENTS"
        };

        public SingaporeDollar() : base(CultureInfo.GetCultureInfo("en-SG"))
        {
        }

        public override string Name => SgdName;
        public override string Code => SgdCode;
        public override string Symbol => SgdSymbol;
        public override CurrencySymbolPlacement SymbolPlacement => CurrencySymbolPlacement.BeforeSum;
        public override IReadOnlyCollection<string> UnitShortForms => mainUnitShortForms;
        public override IReadOnlyCollection<string> SubunitShortForms => fractionalUnitShortForms;
    }

    public sealed class ThaiBaht : Currency
    {
        private const string ThbName = "Thai baht";
        private const string ThbCode = "THB";
        private const string ThbSymbol = "฿";

        private static readonly IReadOnlyCollection<string> mainUnitShortForms = new[]
        {
            ThbSymbol, ThbCode, "BAHT"
        };

        private static readonly IReadOnlyCollection<string> fractionalUnitShortForms = new[]
        {
            "SATANG"
        };

        public ThaiBaht() : base(CultureInfo.GetCultureInfo("th-TH"))
        {
        }

        public override string Name => ThbName;
        public override string Code => ThbCode;
        public override string Symbol => ThbSymbol;
        public override CurrencySymbolPlacement SymbolPlacement => CurrencySymbolPlacement.BeforeSum;
        public override IReadOnlyCollection<string> UnitShortForms => mainUnitShortForms;
        public override IReadOnlyCollection<string> SubunitShortForms => fractionalUnitShortForms;
    }

    public sealed class TajikistaniSomoni : Currency
    {
        private const string TjsName = "Tajikistani somoni";
        private const string TjsCode = "TJS";
        private const string TjsSymbol = "сом";

        private static readonly IReadOnlyCollection<string> mainUnitShortForms = new[]
        {
            TjsSymbol, TjsCode, "SM"
        };

        private static readonly IReadOnlyCollection<string> fractionalUnitShortForms = new[]
        {
            "DIRAM", "DIRAMS"
        };

        public TajikistaniSomoni() : base(CultureInfo.GetCultureInfo("tg-TJ"))
        {
        }

        public override string Name => TjsName;
        public override string Code => TjsCode;
        public override string Symbol => TjsSymbol;
        public override CurrencySymbolPlacement SymbolPlacement => CurrencySymbolPlacement.AfterSum;
        public override IReadOnlyCollection<string> UnitShortForms => mainUnitShortForms;
        public override IReadOnlyCollection<string> SubunitShortForms => fractionalUnitShortForms;
    }

    public sealed class TurkmenistanManat : Currency
    {
        private const string TmtName = "Turkmenistan manat";
        private const string TmtCode = "TMT";
        private const string TmtSymbol = "m";

        private static readonly IReadOnlyCollection<string> mainUnitShortForms = new[]
        {
            TmtSymbol, TmtCode
        };

        private static readonly IReadOnlyCollection<string> fractionalUnitShortForms = new[]
        {
            "TENNESI", "TENNE"
        };

        public TurkmenistanManat() : base(CultureInfo.GetCultureInfo("tk-TM"))
        {
        }

        public override string Name => TmtName;
        public override string Code => TmtCode;
        public override string Symbol => TmtSymbol;
        public override CurrencySymbolPlacement SymbolPlacement => CurrencySymbolPlacement.AfterSum;
        public override IReadOnlyCollection<string> UnitShortForms => mainUnitShortForms;
        public override IReadOnlyCollection<string> SubunitShortForms => fractionalUnitShortForms;
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
        public override IReadOnlyCollection<string> UnitShortForms => mainUnitShortForms;
        public override IReadOnlyCollection<string> SubunitShortForms => fractionalUnitShortForms;
    }

    public sealed class UkrainianHryvnia : Currency
    {
        private const string UahName = "Ukrainian hryvnia";
        private const string UahCode = "UAH";
        private const string UahSymbol = "₴";

        private static readonly IReadOnlyCollection<string> mainUnitShortForms = new[]
        {
            UahSymbol, UahCode, "ГРН"
        };

        private static readonly IReadOnlyCollection<string> fractionalUnitShortForms = new[]
        {
            "КОП", "КОПІЙКА", "KOPIIKA"
        };

        public UkrainianHryvnia() : base(CultureInfo.GetCultureInfo("uk-UA"))
        {
        }

        public override string Name => UahName;
        public override string Code => UahCode;
        public override string Symbol => UahSymbol;
        public override CurrencySymbolPlacement SymbolPlacement => CurrencySymbolPlacement.BeforeSum;
        public override IReadOnlyCollection<string> UnitShortForms => mainUnitShortForms;
        public override IReadOnlyCollection<string> SubunitShortForms => fractionalUnitShortForms;
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
        public override IReadOnlyCollection<string> UnitShortForms => mainUnitShortForms;
        public override IReadOnlyCollection<string> SubunitShortForms => fractionalUnitShortForms;
    }

    public sealed class UzbekistanSom : Currency
    {
        private const string UzsName = "Uzbekistan som";
        private const string UzsCode = "UZS";
        private const string UzsSymbol = "soʻm";

        private static readonly IReadOnlyCollection<string> mainUnitShortForms = new[]
        {
            UzsSymbol, UzsCode, "сўм"
        };

        private static readonly IReadOnlyCollection<string> fractionalUnitShortForms = new[]
        {
            "TIYIN", "TIYINS"
        };

        public UzbekistanSom() : base(CultureInfo.GetCultureInfo("uz-UZ"))
        {
        }

        public override string Name => UzsName;
        public override string Code => UzsCode;
        public override string Symbol => UzsSymbol;
        public override CurrencySymbolPlacement SymbolPlacement => CurrencySymbolPlacement.AfterSum;
        public override IReadOnlyCollection<string> UnitShortForms => mainUnitShortForms;
        public override IReadOnlyCollection<string> SubunitShortForms => fractionalUnitShortForms;
    }

    public sealed class VietnameseDong : Currency
    {
        private const string VndName = "Vietnamese dong";
        private const string VndCode = "VND";
        private const string VndSymbol = "₫";

        private static readonly IReadOnlyCollection<string> mainUnitShortForms = new[]
        {
            VndSymbol, VndCode
        };

        private static readonly IReadOnlyCollection<string> fractionalUnitShortForms = new[]
        {
            "XU"
        };

        public VietnameseDong() : base(CultureInfo.GetCultureInfo("vi-VN"))
        {
        }

        public override string Name => VndName;
        public override string Code => VndCode;
        public override string Symbol => VndSymbol;
        public override CurrencySymbolPlacement SymbolPlacement => CurrencySymbolPlacement.AfterSum;
        public override IReadOnlyCollection<string> UnitShortForms => mainUnitShortForms;
        public override IReadOnlyCollection<string> SubunitShortForms => fractionalUnitShortForms;
    }

    public sealed class SpecialDrawingRights : Currency
    {
        private const string XdrName = "Special drawing rights";
        private const string XdrCode = "XDR";

        private static readonly IReadOnlyCollection<string> mainUnitShortForms = new[]
        {
            "SDR", XdrCode
        };

        private static readonly IReadOnlyCollection<string> fractionalUnitShortForms = Array.Empty<string>();

        public SpecialDrawingRights() : base(CultureInfo.InvariantCulture)
        {
        }

        public override string Name => XdrName;
        public override string Code => XdrCode;
        public override CurrencySymbolPlacement SymbolPlacement => CurrencySymbolPlacement.BeforeSum;
        public override IReadOnlyCollection<string> UnitShortForms => mainUnitShortForms;
        public override IReadOnlyCollection<string> SubunitShortForms => fractionalUnitShortForms;
    }

    public sealed class SouthAfricanRand : Currency
    {
        private const string ZarName = "South African rand";
        private const string ZarCode = "ZAR";
        private const string ZarSymbol = "R";

        private static readonly IReadOnlyCollection<string> mainUnitShortForms = new[]
        {
            ZarSymbol, ZarCode, "RAND"
        };

        private static readonly IReadOnlyCollection<string> fractionalUnitShortForms = new[]
        {
            "CENT", "CENTS"
        };

        public SouthAfricanRand() : base(CultureInfo.GetCultureInfo("en-ZA"))
        {
        }

        public override string Name => ZarName;
        public override string Code => ZarCode;
        public override string Symbol => ZarSymbol;
        public override CurrencySymbolPlacement SymbolPlacement => CurrencySymbolPlacement.BeforeSum;
        public override IReadOnlyCollection<string> UnitShortForms => mainUnitShortForms;
        public override IReadOnlyCollection<string> SubunitShortForms => fractionalUnitShortForms;
    }
}