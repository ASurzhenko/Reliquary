using System;

namespace Reliquary.Domain
{
    /// <summary>
    /// Stable identity of a relic. Content assets carry it, the domain compares it, and nothing about
    /// how a relic looks or is stored is visible from here.
    /// </summary>
    public readonly struct RelicId : IEquatable<RelicId>
    {
        private readonly string _value;

        public RelicId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("A relic id must be a non-empty string.", nameof(value));
            }

            _value = value;
        }

        public bool IsValid => !string.IsNullOrWhiteSpace(_value);

        public static bool operator ==(RelicId left, RelicId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(RelicId left, RelicId right)
        {
            return !left.Equals(right);
        }

        public bool Equals(RelicId other)
        {
            return string.Equals(_value, other._value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is RelicId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _value == null ? 0 : StringComparer.Ordinal.GetHashCode(_value);
        }

        public override string ToString()
        {
            return _value ?? string.Empty;
        }
    }
}
