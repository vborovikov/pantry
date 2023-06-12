namespace Pantry
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class MeasureExtensions
    {
        public static Measure Average(this IEnumerable<Measure> source)
        {
            var unit = GetUnit(source);
            return new Measure(source.Select(m => m.Value).Average(f => f.Value), unit);
        }

        public static Measure Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, Measure> selector)
        {
            return Sum(source.Select(m => selector(m)).ToArray());
        }

        public static Measure Sum(this IEnumerable<Measure> source)
        {
            //todo: one pass to find the sum

            var unit = source
                .Select(m => m.Unit)
                .Distinct()
                .Single();

            return new Measure(source.Select(m => m.Value).Sum(f => f.Value), unit);
        }

        private static MeasureUnit GetUnit(IEnumerable<Measure> source)
        {
            return source
                .Select(m => m.Unit)
                .Distinct()
                .Single();
        }
    }
}