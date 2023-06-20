#define FRACTIONAL_PARSE_FOOD

namespace Pantry
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// The main Fractional struct that allows parsing and working with fractinal expressions.
    /// </summary>
    public readonly struct Fractional : IEquatable<Fractional>, IEquatable<float>,
        IComparable<Fractional>, IComparable<float>, ISpanFormattable, ISpanParsable<Fractional>, IFiniteSpanParsable<Fractional>
    {
        private enum CompositionPartCategory
        {
            None,
            Sign,
            Bar,
            Number,
            OverNumber,
            UnderNumber,
            Vulgar,
            RealNumber,
        }

        private enum ParsingStep
        {
            Sign,
            Integer,
            Numerator,
            Bar,
            Denominator,
            Vulgar,
        }

        private readonly ref struct CompositionPart
        {
            public CompositionPart(ReadOnlySpan<char> span, int index, CompositionPartCategory category)
            {
                this.Span = span;
                this.Index = index;
                this.Category = category;
            }

            public ReadOnlySpan<char> Span { get; }
            public int Index { get; }
            public CompositionPartCategory Category { get; }
        }

        private ref struct CompositionEnumerator
        {
            private ReadOnlySpan<char> writing;
            private int index;
            private readonly NumberFormatInfo numberFormat;

            public CompositionEnumerator(ReadOnlySpan<char> writing, IFormatProvider? formatProvider)
            {
                this.writing = writing;
                this.index = 0;
                this.Current = default;
                this.numberFormat =
                    formatProvider as NumberFormatInfo ??
                    formatProvider?.GetFormat(typeof(NumberFormatInfo)) as NumberFormatInfo ??
                    NumberFormatInfo.CurrentInfo;
            }

            public CompositionPart Current { get; private set; }

            public CompositionEnumerator GetEnumerator() => this;

            public bool MoveNext()
            {
                if (this.writing.IsEmpty)
                    return false;

                var start = 0;
                for (; start != this.writing.Length; ++start)
                {
                    if (!char.IsWhiteSpace(this.writing[start]))
                        break;
                }
                if (start == this.writing.Length)
                    return false;

                var part = this.writing[start] switch
                {
                    var symbol when Signs.Contains(symbol) => CompositionPartCategory.Sign,
                    Virgule or Solidus or Backslash => CompositionPartCategory.Bar,
                    var digit when Digits.Contains(digit) => CompositionPartCategory.Number,
                    var point when this.numberFormat.NumberDecimalSeparator.Contains(point) => CompositionPartCategory.RealNumber,
                    var digit when SupDigits.Contains(digit) => CompositionPartCategory.OverNumber,
                    var digit when SubDigits.Contains(digit) => CompositionPartCategory.UnderNumber,
                    var fraction when Vulgars.Contains(fraction) => CompositionPartCategory.Vulgar,
                    _ => CompositionPartCategory.None
                };

                if (part == CompositionPartCategory.None)
                    return false;

                var end = start + 1;
                if (part == CompositionPartCategory.Number)
                {
                    end = this.writing.ClampStart(end, Digits);
                    if (end < this.writing.Length && this.numberFormat.NumberDecimalSeparator.Contains(this.writing[end]))
                    {
                        var newEnd = end + 1;
                        if (newEnd < this.writing.Length)
                        {
                            part = CompositionPartCategory.RealNumber;
                            end = this.writing.ClampStart(newEnd, Digits);
                        }
                    }
                }
                else if (part == CompositionPartCategory.RealNumber)
                {
                    end = this.writing.ClampStart(end, Digits);
                }
                else if (part == CompositionPartCategory.OverNumber)
                {
                    end = this.writing.ClampStart(end, SupDigits);
                }
                else if (part == CompositionPartCategory.UnderNumber)
                {
                    end = this.writing.ClampStart(end, SubDigits);
                }

                this.Current = new CompositionPart(this.writing[start..end], this.index + start, part);
                this.writing = this.writing[end..];
                this.index += end;

                return true;
            }
        }

        public static readonly Fractional NaN = new(0, 0);
        public static readonly Fractional Zero = new(0, 1);
        public static readonly Fractional One = new(1, 1);

        private const char Backslash = '\\';
        private const char Virgule = '/';
        private const char Solidus = '⁄';
#if FRACTIONAL_PARSE_FOOD
        private const string Signs = "+-&_";
#else
        private const string Signs = "+-⁺⁻₊₋";
#endif
        private const string Digits = "0123456789";
        private const string SupDigits = "⁰¹²³⁴⁵⁶⁷⁸⁹";
        private const string SubDigits = "₀₁₂₃₄₅₆₇₈₉";
        private const string Vulgars = "¼½¾⅐⅑⅒⅓⅔⅕⅖⅗⅘⅙⅚⅛⅜⅝⅞↉";
        private const float AccuracyFactor = 0.01f;

        private readonly int numerator;
        private readonly int denominator;

        //public Fractional()
        //    : this(0, 1)
        //{
        //}

        /// <summary>
        /// Builds the fractional from a given numerator and denominator.
        /// </summary>
        /// <param name="num">The numerator.</param>
        /// <param name="denom">The denominator should be different from zero</param>
        private Fractional(int num, int denom)
        {
            if (denom != 0)
            {
                var gcd = ComputeGreatestCommonDivisor(Math.Abs(num), Math.Abs(denom));
                if (gcd <= denom)
                {
                    num /= gcd;
                    denom /= gcd;
                }
            }

            this.numerator = num;
            this.denominator = denom;
        }

        /// <summary>
        /// Gets the actual value of the fraction.
        /// </summary>
        public float Value
        {
            get
            {
                if (IsNaN(this))
                    throw new NotFiniteNumberException();

                return (float)this.numerator / this.denominator;
            }
        }

        /// <summary>
        /// Gets the numerator value.
        /// </summary>
        public int Numerator => this.numerator;

        /// <summary>
        /// Gets the denominator value.
        /// </summary>
        public int Denominator => this.denominator;

        public bool IsInteger => (this.numerator % this.denominator) == 0;

        /// <summary>
        /// Retrun an indication wether the object is valid number or not.
        /// </summary>
        public static bool IsNaN(Fractional fractional) => fractional.denominator == 0;

        public static Fractional Parse(string s, IFormatProvider? provider) =>
            Parse(s.AsSpan(), provider);

        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out Fractional result) =>
            TryParse(s.AsSpan(), provider, out result);

        public static Fractional Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
        {
            if (TryParse(s, provider, out var result, out _))
            {
                return result;
            }

            throw new FormatException();
        }

        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out Fractional result) =>
            TryParse(s, provider, out result, out _);

        public static Fractional Parse(ReadOnlySpan<char> span) =>
            Parse(span, null);

        public static bool TryParse(ReadOnlySpan<char> span, out Fractional result) =>
            TryParse(span, null, out result);

        public static bool TryParse(ReadOnlySpan<char> span, IFormatProvider? provider, [MaybeNullWhen(false)] out Fractional result, out int charsConsumed)
        {
            charsConsumed = 0;

            if (span.IsEmpty)
            {
                result = default;
                return false;
            }

            var sign = 1;
            var integer = 0;
            var numerator = 0;
            var denominator = 1;
            var parsingStep = ParsingStep.Sign;
            Span<char> number = stackalloc char[8];
            foreach (var part in Decompose(span, provider))
            {
                if (parsingStep > ParsingStep.Vulgar)
                {
                    result = default;
                    return false;
                }

                charsConsumed = part.Index + part.Span.Length;
                if (part.Category == CompositionPartCategory.RealNumber)
                {
                    if (Single.TryParse(part.Span, NumberStyles.Float, provider, out var realNumber))
                    {
                        result = realNumber;
                        return true;
                    }

                    result = default;
                    return false;
                }

                switch (parsingStep)
                {
                    case ParsingStep.Sign:
                        if (part.Category == CompositionPartCategory.Sign)
                        {
                            sign = part.Span[0] switch
                            {
                                '-' => -1,
                                '+' => 1,
                                _ => 0
                            };

                            if (sign == 0)
                            {
                                result = default;
                                return false;
                            }
                        }
                        else
                        {
                            ++parsingStep;
                            goto case ParsingStep.Integer;
                        }
                        break;

                    case ParsingStep.Integer:
                        if (part.Category == CompositionPartCategory.Number)
                        {
                            if (!Int32.TryParse(part.Span, NumberStyles.Integer, provider, out integer))
                            {
                                result = default;
                                return false;
                            }
                        }
                        else if (part.Category == CompositionPartCategory.OverNumber ||
                            part.Category == CompositionPartCategory.Vulgar)
                        {
                            ++parsingStep;
                            goto case ParsingStep.Numerator;
                        }
                        else
                        {
                            result = default;
                            return false;
                        }
                        break;

                    case ParsingStep.Numerator:
                        if (part.Category == CompositionPartCategory.Number)
                        {
                            if (!Int32.TryParse(part.Span, NumberStyles.Integer, provider, out numerator))
                            {
                                result = default;
                                return false;
                            }
                        }
                        else if (part.Category == CompositionPartCategory.OverNumber)
                        {
                            for (var i = 0; i != part.Span.Length; ++i)
                            {
                                if (i == number.Length)
                                    break;
                                number[i] = Digits[SupDigits.IndexOf(part.Span[i])];
                            }
                            if (!Int32.TryParse(number[..part.Span.Length], NumberStyles.Integer, provider, out numerator))
                            {
                                result = default;
                                return false;
                            }
                        }
                        else if (part.Category == CompositionPartCategory.Bar)
                        {
                            ++parsingStep;
                            numerator = integer;
                            integer = 0;
                        }
                        else if (part.Category == CompositionPartCategory.Vulgar)
                        {
                            parsingStep = ParsingStep.Vulgar;
                            goto case ParsingStep.Vulgar;
                        }
#if FRACTIONAL_PARSE_FOOD
                        else if (part.Span.Length == 1 && char.IsPunctuation(part.Span[0]))
                        {
                            continue;
                        }
#endif
                        else
                        {
                            result = default;
                            return false;
                        }
                        break;

                    case ParsingStep.Bar:
                        if (part.Category != CompositionPartCategory.Bar)
                        {
                            result = default;
                            return false;
                        }
                        break;

                    case ParsingStep.Denominator:
                        if (part.Category == CompositionPartCategory.Number)
                        {
                            if (!Int32.TryParse(part.Span, NumberStyles.Integer, provider, out denominator))
                            {
                                result = default;
                                return false;
                            }
                        }
                        else if (part.Category == CompositionPartCategory.UnderNumber)
                        {
                            for (var i = 0; i != part.Span.Length; ++i)
                            {
                                if (i == number.Length)
                                    break;
                                number[i] = Digits[SubDigits.IndexOf(part.Span[i])];
                            }
                            if (!Int32.TryParse(number[..part.Span.Length], NumberStyles.Integer, provider, out denominator))
                            {
                                result = default;
                                return false;
                            }
                        }
                        else if (part.Category != CompositionPartCategory.Vulgar)
                        {
                            result = default;
                            return false;
                        }
                        break;

                    case ParsingStep.Vulgar:
                        if (part.Category == CompositionPartCategory.Vulgar)
                        {
                            // https://unicode-search.net/unicode-namesearch.pl?term=VULGAR%20FRACTION
                            // https://qaz.wtf/u/fraction.cgi

                            var charFraction = part.Span[0] switch
                            {
                                '¼' => (1, 4),
                                '½' => (1, 2),
                                '¾' => (3, 4),
                                '⅐' => (1, 7),
                                '⅑' => (1, 9),
                                '⅒' => (1, 10),
                                '⅓' => (1, 3),
                                '⅔' => (2, 3),
                                '⅕' => (1, 5),
                                '⅖' => (2, 5),
                                '⅗' => (3, 5),
                                '⅘' => (4, 5),
                                '⅙' => (1, 6),
                                '⅚' => (5, 6),
                                '⅛' => (1, 8),
                                '⅜' => (3, 8),
                                '⅝' => (5, 8),
                                '⅞' => (7, 8),
                                '↉' => (0, 3),
                                _ => (0, 0),
                            };

                            numerator = charFraction.Item1;
                            denominator = charFraction.Item2;
                        }
#if FRACTIONAL_PARSE_FOOD
                        else if (part.Span.Length == 1 && char.IsPunctuation(part.Span[0]))
                        {
                            continue;
                        }
#endif
                        else
                        {
                            result = default;
                            return false;
                        }
                        break;
                }

                ++parsingStep;
            }

            if (charsConsumed > 0)
            {
                result = new Fractional((Math.Abs(integer) * denominator + numerator) * sign, denominator);
                return !IsNaN(result);
            }

            result = default;
            return false;
        }

        public static implicit operator Fractional(int value) => From(value);

        public static implicit operator Fractional(float value) => From(value);

        public static Fractional operator +(Fractional left, float number)
        {
            return Addition(left, From(number));
        }

        public static Fractional operator +(Fractional left, Fractional right)
        {
            return Addition(left, right);
        }

        public static Fractional operator -(Fractional left, float number)
        {
            return Subtraction(left, From(number));
        }

        public static Fractional operator -(Fractional left, Fractional right)
        {
            return Subtraction(left, right);
        }

        public static Fractional operator *(Fractional left, float number)
        {
            return Multiplication(left, From(number));
        }

        public static Fractional operator *(Fractional left, Fractional right)
        {
            return Multiplication(left, right);
        }

        public static Fractional operator /(Fractional left, float number)
        {
            return Division(left, From(number));
        }

        public static Fractional operator /(Fractional left, Fractional right)
        {
            return Division(left, right);
        }

        public static bool operator ==(Fractional left, Fractional right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Fractional left, Fractional right)
        {
            return !left.Equals(right);
        }

        public static bool operator <=(Fractional left, Fractional right)
        {
            return left.CompareTo(right) <= 0;
        }

        public static bool operator >=(Fractional left, Fractional right)
        {
            return left.CompareTo(right) >= 0;
        }

        public static bool operator <(Fractional left, Fractional right)
        {
            return left.CompareTo(right) < 0;
        }

        public static bool operator >(Fractional left, Fractional right)
        {
            return left.CompareTo(right) > 0;
        }

        public static bool operator ==(Fractional left, float right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Fractional left, float right)
        {
            return !left.Equals(right);
        }

        public static bool operator <=(Fractional left, float right)
        {
            return left.CompareTo(right) <= 0;
        }

        public static bool operator >=(Fractional left, float right)
        {
            return left.CompareTo(right) >= 0;
        }

        public static bool operator <(Fractional left, float right)
        {
            return left.CompareTo(right) < 0;
        }

        public static bool operator >(Fractional left, float right)
        {
            return left.CompareTo(right) > 0;
        }

        /// <summary>
        /// Return the human readable form of the fraction
        /// </summary>
        public override string ToString() => ToString(null, null);

        public string ToString(string format) => ToString(format, null);

        public string ToString(string? format, IFormatProvider? formatProvider)
        {
            return String.Create(
                GetFormattedLength(this.numerator, this.denominator, format, formatProvider),
                (this.numerator, this.denominator, format, formatProvider),
                (buffer, state) => Format(state.numerator, state.denominator, buffer, state.format, state.formatProvider));
        }

        public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
        {
            charsWritten = Format(this.numerator, this.denominator, destination, format, provider);
            if (charsWritten > 0)
                return true;

            charsWritten = destination.Length;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CountDigits(int n)
        {
            return n == 0 ? 1 : (n > 0 ? 1 : 2) + (int)Math.Log10(Math.Abs((double)n));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static char GetVulgarChar(int numerator, int denominator) => (numerator, denominator) switch
        {
            (1, 4) => '¼',
            (1, 2) => '½',
            (3, 4) => '¾',
            (1, 7) => '⅐',
            (1, 9) => '⅑',
            (1, 10) => '⅒',
            (1, 3) => '⅓',
            (2, 3) => '⅔',
            (1, 5) => '⅕',
            (2, 5) => '⅖',
            (3, 5) => '⅗',
            (4, 5) => '⅘',
            (1, 6) => '⅙',
            (5, 6) => '⅚',
            (1, 8) => '⅛',
            (3, 8) => '⅜',
            (5, 8) => '⅝',
            (7, 8) => '⅞',
            (0, 3) => '↉',
            _ => '\0'
        };

        private static int GetFormattedLength(int numerator, int denominator, ReadOnlySpan<char> format, IFormatProvider? formatProvider)
        {
            if (denominator == 0)
                throw new NotFiniteNumberException();

            if (numerator == 0)
            {
                return 1;
            }
            else if (denominator == 1)
            {
                return CountDigits(numerator);
            }
            else if (numerator == denominator)
            {
                return 1;
            }
            else
            {
                var integer = numerator / denominator;
                var numeratorNorm = numerator < denominator ? numerator : numerator % denominator;
                var denominatorNorm = denominator;

                var useDefaultFormat = format.IsEmpty || (format.Length == 1 && (format[0] == 'G' || format[0] == 'g'));
                var useVulgarFormat = format.Length == 1 && (format[0] == 'V' || format[0] == 'v');

                // #_#/#

                if (useVulgarFormat && GetVulgarChar(numeratorNorm, denominatorNorm) != '\0')
                {
                    return (integer != 0 ? CountDigits(integer) : 0) + 1;
                }

                return
                    (integer != 0 ? CountDigits(integer) + (useDefaultFormat ? 1 : 0) : 0) +
                    CountDigits(numeratorNorm) +
                    1 +
                    CountDigits(denominatorNorm);
            }
        }

        private static int Format(int numerator, int denominator, Span<char> destination, ReadOnlySpan<char> format, IFormatProvider? provider)
        {
            static void PlaceDigits(int n, Span<char> buffer, int end, ReadOnlySpan<char> digits)
            {
                var hasMinus = n < 0;
                if (hasMinus)
                    n = -n;
                do
                {
                    buffer[--end] = digits[n % 10];
                    n /= 10;
                } while (n != 0);

                if (hasMinus)
                    buffer[--end] = '-';
            }

            if (denominator == 0)
                throw new NotFiniteNumberException();

            if (numerator == 0)
            {
                destination[0] = '0';
                return 1;
            }
            else if (denominator == 1)
            {
                numerator.TryFormat(destination, out var charsWritten, format, provider);
                return charsWritten;
            }
            else if (numerator == denominator)
            {
                destination[0] = '1';
                return 1;
            }
            else
            {
                var integer = numerator / denominator;
                var numeratorNorm = numerator < denominator ? numerator : numerator % denominator;
                var denominatorNorm = denominator;

                var integerLength = integer != 0 ? CountDigits(integer) : 0;
                var numeratorLength = CountDigits(numeratorNorm);
                var denominatorLength = CountDigits(denominatorNorm);
                var useDefaultFormat = format.IsEmpty || (format.Length == 1 && (format[0] == 'G' || format[0] == 'g'));
                var useVulgarFormat = format.Length == 1 && (format[0] == 'V' || format[0] == 'v');
                var vulgarChar = useVulgarFormat ? GetVulgarChar(numeratorNorm, denominatorNorm) : '\0';
                var separatorLength = (useDefaultFormat && integerLength > 0 ? 1 : 0);
                var solidusLength = 1;
                var formattedLength = useVulgarFormat && vulgarChar != '\0' ?
                    integerLength + 1 :
                    integerLength + separatorLength + numeratorLength + solidusLength + denominatorLength;

                if (formattedLength > destination.Length)
                    return 0;

                var digits = Digits;
                var supDigits = useDefaultFormat ? Digits : SupDigits;
                var subDigits = useDefaultFormat ? Digits : SubDigits;

                if (integer != 0)
                {
                    PlaceDigits(integer, destination, integerLength, digits);
                    if (useDefaultFormat)
                    {
                        destination[integerLength] = ' ';
                    }
                }
                if (useVulgarFormat && vulgarChar != '\0')
                {
                    destination[integerLength] = vulgarChar;
                }
                else
                {
                    PlaceDigits(numeratorNorm, destination, integerLength + separatorLength + numeratorLength, supDigits);
                    destination[integerLength + separatorLength + numeratorLength] = useDefaultFormat ? Virgule : Solidus;
                    PlaceDigits(denominatorNorm, destination, formattedLength, subDigits);
                }

                return formattedLength;
            }
        }

        public override bool Equals(object? obj)
        {
            return obj switch
            {
                Fractional fr => Equals(fr),
                float fl => Equals(fl),
                _ => false,
            };
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.numerator, this.denominator);
        }

        public bool Equals(Fractional other)
        {
            if (this.numerator == other.numerator && this.denominator == other.denominator)
                return true;

            return IsNaN(other) && IsNaN(this);
        }

        public int CompareTo(Fractional other)
        {
            if (IsNaN(this) && IsNaN(other))
                return 0;
            if (IsNaN(this))
                return -1;
            if (IsNaN(other))
                return 1;

            if (this.Value < other.Value)
                return -1;
            if (this.Value > other.Value)
                return 1;

            return 0;
        }

        public bool Equals(float other)
        {
            if (IsNaN(this) && Single.IsNaN(other))
                return true;

            return this.Value.Equals(other);
        }

        public int CompareTo(float other)
        {
            if (IsNaN(this) && Single.IsNaN(other))
                return 0;
            if (IsNaN(this))
                return -1;
            if (Single.IsNaN(other))
                return 1;

            if (this.Value < other)
                return -1;
            if (this.Value > other)
                return 1;

            return 0;
        }

        private static CompositionEnumerator Decompose(ReadOnlySpan<char> writing, IFormatProvider? formatProvider) =>
            new CompositionEnumerator(writing, formatProvider);

        private static Fractional Addition(Fractional left, Fractional right)
        {
            var commonDenominator = left.Denominator * right.Denominator;

            var leftNumerator = left.Numerator * right.Denominator;
            var rightNumerator = right.Numerator * left.Denominator;

            return new Fractional(leftNumerator + rightNumerator, commonDenominator);
        }

        private static Fractional Subtraction(Fractional left, Fractional right)
        {
            var commonDenominator = left.Denominator * right.Denominator;

            var leftNumerator = left.Numerator * right.Denominator;
            var rightNumerator = right.Numerator * left.Denominator;

            return new Fractional(leftNumerator - rightNumerator, commonDenominator);
        }

        private static Fractional Multiplication(Fractional left, Fractional right)
        {
            return new Fractional(left.Numerator * right.Numerator, left.Denominator * right.Denominator);
        }

        private static Fractional Division(Fractional left, Fractional right)
        {
            return new Fractional(left.Numerator * right.Denominator, left.Denominator * right.Numerator);
        }

        private static int ComputeGreatestCommonDivisor(int a, int b)
        {
            while (a != 0 && b != 0)
            {
                if (a > b)
                {
                    a %= b;
                }
                else
                {
                    b %= a;
                }
            }

            return a == 0 ? b : a;
        }

        private static Fractional From(float number)
        {
            var sign = number >= 0f ? 1 : -1;
            number = Math.Abs(number);

            if (number % 1 == 0f)
            {
                return new Fractional(Convert.ToInt32(number * sign), 1);
            }

            if (number <= Single.MinValue)
            {
                return new Fractional(sign, Int32.MaxValue);
            }

            if (number >= Single.MaxValue)
            {
                return new Fractional(Int32.MaxValue * sign, 1);
            }

            var fractNumerator = 0.0f;
            var fractDenominator = 1.0f;
            var Z = number;
            var prevDenominator = 0.0f;
            do
            {
                Z = 1.0f / (Z - Convert.ToInt32(Z));
                var scratchValue = fractDenominator;
                fractDenominator = fractDenominator * Convert.ToInt32(Z) + prevDenominator;
                prevDenominator = scratchValue;

                fractNumerator = Convert.ToInt32(number * fractDenominator + 0.5f - AccuracyFactor);
            } while (Math.Abs(number - (fractNumerator / fractDenominator)) > AccuracyFactor && (Z % 1 != 0f));

            var numerator = Convert.ToInt32(fractNumerator) * sign;
            var denominator = Convert.ToInt32(fractDenominator);

            return new Fractional(numerator, denominator);
        }
    }
}