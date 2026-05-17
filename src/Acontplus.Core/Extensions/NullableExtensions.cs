namespace Acontplus.Core.Extensions;

/// <summary>Extension methods for working with nullable reference types.</summary>
public static class NullableExtensions
{
    /// <summary>Returns <c>true</c> when the value is <c>null</c>.</summary>
    public static bool IsNull<T>(this T? value) where T : class => value is null;

    /// <summary>Returns <c>true</c> when the value is not <c>null</c>.</summary>
    public static bool IsNotNull<T>(this T? value) where T : class => value is not null;

    /// <summary>Returns <paramref name="value"/> when non-null; otherwise returns <paramref name="defaultValue"/>.</summary>
    public static T OrDefault<T>(this T? value, T defaultValue) where T : class => value ?? defaultValue;

    /// <summary>Returns <paramref name="value"/> when non-null; otherwise throws <paramref name="exception"/>.</summary>
    public static T OrThrow<T>(this T? value, Exception exception) where T : class => value ?? throw exception;

    /// <summary>Returns <paramref name="value"/> when non-null; otherwise throws the exception produced by <paramref name="exceptionFactory"/>.</summary>
    public static T OrThrow<T>(this T? value, Func<Exception> exceptionFactory) where T : class => value ?? throw exceptionFactory();

    /// <summary>Returns <paramref name="value"/> when non-null; otherwise throws an <see cref="ArgumentNullException"/> with <paramref name="message"/>.</summary>
    public static T OrThrow<T>(this T? value, string message) where T : class => value ?? throw new ArgumentNullException(nameof(value), message);

    /// <summary>Returns <paramref name="value"/> when non-null; otherwise throws an <see cref="ArgumentNullException"/> whose message is produced by <paramref name="messageFactory"/>.</summary>
    public static T OrThrow<T>(this T? value, Func<string> messageFactory) where T : class => value ?? throw new ArgumentNullException(nameof(value), messageFactory());
}
