namespace Pantry
{
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

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