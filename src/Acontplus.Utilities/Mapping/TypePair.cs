namespace Acontplus.Utilities.Mapping;

/// <summary>
/// Identifies a unique mapping route from <see cref="SourceType"/> to <see cref="TargetType"/>.
/// Implemented as a value type to allow efficient use as a dictionary key.
/// </summary>
public readonly struct TypePair : IEquatable<TypePair>
{
    /// <summary>The source CLR type.</summary>
    public Type SourceType { get; }

    /// <summary>The target CLR type.</summary>
    public Type TargetType { get; }

    /// <summary>Initialises a new <see cref="TypePair"/>.</summary>
    /// <param name="sourceType">The source CLR type.</param>
    /// <param name="targetType">The target CLR type.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="sourceType"/> or <paramref name="targetType"/> is <c>null</c>.
    /// </exception>
    public TypePair(Type sourceType, Type targetType)
    {
        SourceType = sourceType ?? throw new ArgumentNullException(nameof(sourceType));
        TargetType = targetType ?? throw new ArgumentNullException(nameof(targetType));
    }

    /// <inheritdoc />
    public bool Equals(TypePair other) =>
        SourceType == other.SourceType && TargetType == other.TargetType;

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        obj is TypePair other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() =>
        HashCode.Combine(SourceType, TargetType);

    /// <inheritdoc />
    public override string ToString() =>
        $"{SourceType.Name} => {TargetType.Name}";

    /// <summary>Determines whether two <see cref="TypePair"/> instances are equal.</summary>
    public static bool operator ==(TypePair left, TypePair right) => left.Equals(right);

    /// <summary>Determines whether two <see cref="TypePair"/> instances are not equal.</summary>
    public static bool operator !=(TypePair left, TypePair right) => !left.Equals(right);
}
