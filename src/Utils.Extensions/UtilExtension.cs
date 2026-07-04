using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace zms9110750.Extensions.Utils;

public static class UtilExtension
{

    private static readonly ImmutableHashSet<char> _invalidFileNameChars = Path.GetInvalidFileNameChars().ToImmutableHashSet();

    public static string ToSafeFileName(this string s, char replacement = '_')
    {
        if (string.IsNullOrEmpty(s))
        {
            return s;
        }

        return string.Create(s.Length, (s, replacement), static (span, state) => {
            state.s.CopyTo(span);
            foreach (ref var item in span)
            {
                if (_invalidFileNameChars.Contains(item))
                {
                    item = state.replacement;
                }
            }
        });
    }

    public static string ToString<T>(this IEnumerable<T> values, string separator = ", ")
    {
        return separator switch {
            "" or null => string.Concat(values),
            [char c] => string.Join(c, values),
            _ => string.Join(separator, values),
        };
    }
    extension(ArgumentOutOfRangeException)
    {
        /// <summary>
        /// 检查值是否在指定范围内，如果超出范围则抛出 <see cref="ArgumentOutOfRangeException"/>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ThrowIfOutOfRange<T>(T value, T min, T max, string? message = null,
#if NET6_0_OR_GREATER
            [CallerArgumentExpression(nameof(value))]
#endif
            string? paramName = null) where T : IComparable<T>
        {
            return value.CompareTo(min) < 0 || value.CompareTo(max) > 0
                ? throw new ArgumentOutOfRangeException(paramName, value, message ?? $"参数必须在 {min} 和 {max} 之间")
                : value;
        }
    }
}
