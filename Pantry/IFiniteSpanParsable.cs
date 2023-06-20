namespace Pantry;

using System;
using System.Diagnostics.CodeAnalysis;

/// <summary>Defines a mechanism for parsing a span of characters to a value.</summary>
/// <typeparam name="TSelf">The type that implements this interface.</typeparam>
public interface IFiniteSpanParsable<TSelf> : ISpanParsable<TSelf> where TSelf : IFiniteSpanParsable<TSelf>?
{
    /// <summary>Tries to parse a span of characters into a value.</summary>
    /// <param name="s">The span of characters to parse.</param>
    /// <param name="provider">An object that provides culture-specific formatting information about <paramref name="s" />.</param>
    /// <param name="result">On return, contains the result of successfully parsing <paramref name="s" /> or an undefined value on failure.</param>
    /// <param name="charsConsumed">When this method returns, the number of characters read to parse the value.</param>
    /// <returns><c>true</c> if <paramref name="s" /> was successfully parsed; otherwise, <c>false</c>.</returns>
    static abstract bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider,
        [MaybeNullWhen(false)] out TSelf result, out int charsConsumed);
}
