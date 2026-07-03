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
            { Length: 1 } => string.Join(separator[0], values),
            _ => string.Join(separator, values),
        };
    }
    extension(ArgumentOutOfRangeException)
    {
        /// <summary>
        /// 检查值是否在指定范围内，如果超出范围则抛出 <see cref="ArgumentOutOfRangeException"/>
        /// </summary>
        /// <typeparam name="T">实现了 <see cref="IComparable{T}"/> 的类型</typeparam>
        /// <param name="value">要检查的值</param>
        /// <param name="min">最小值（包含）</param>
        /// <param name="max">最大值（包含）</param>
        /// <param name="message">自定义错误消息（可选）</param>
        /// <param name="paramName">参数名（自动获取）</param>
        /// <returns>原值（如果未抛出异常）</returns>
        public static T ThrowIfOutOfRange<T>(T value, T min, T max, string? message = null, [CallerArgumentExpression(nameof(value))] string? paramName = null) where T : IComparable<T>
        {
            return value.CompareTo(min) < 0 || value.CompareTo(max) > 0
                ? throw new ArgumentOutOfRangeException(paramName, value, message ?? $"参数必须在 {min} 和 {max} 之间")
                : value;
        }
    }
}
