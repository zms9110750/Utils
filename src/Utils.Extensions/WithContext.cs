using System.Runtime.CompilerServices;

namespace zms9110750.Extensions.Utils;

public readonly record struct WithContext<T>(
    T Obj,
    [CallerArgumentExpression(nameof(Obj))] string? CallerArgumentExpression = null,
    [CallerMemberName] string? CallerMemberName = null,
    [CallerLineNumber] int? CallerLineNumber = null,
    [CallerFilePath] string? CallerFilePath = null
    )
{
    public override string ToString()
    {
        return $"[{CallerArgumentExpression} = {Obj} ]";
    }
}

public static class WithContextExtension
{
    public static T OutContext<T>(this T Obj,
    out WithContext<T> context,
    [CallerMemberName] string? CallerMemberName = null,
    [CallerLineNumber] int? CallerLineNumber = null,
    [CallerFilePath] string? CallerFilePath = null,
    [CallerArgumentExpression(nameof(Obj))] string? CallerArgumentExpression = null)
    {
        context = new WithContext<T>(Obj, CallerArgumentExpression, CallerMemberName, CallerLineNumber, CallerFilePath);
        return Obj;
    }
}
