using System;

namespace Reliquary.Domain
{
    /// <summary>
    /// Stable identity of a set. A set enumerates the relics that belong to it; a relic knows nothing about
    /// sets, so this id never appears on a relic.
    /// </summary>
    public readonly struct SetId : IEquatable<SetId>
    {
        private readonly string _value;

        public SetId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("A set id must be a non-empty string.", nameof(value));
            }

            _value = value;
        }

        /// <summary>
        /// False for the default value. Also the "is there a focus set at all?" token, so a caller never has
        /// to compare a SetId against default.
        /// </summary>
        public bool IsValid => !string.IsNullOrWhiteSpace(_value);

        public static bool operator ==(SetId left, SetId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SetId left, SetId right)
        {
            return !left.Equals(right);
        }

        public bool Equals(SetId other)
        {
            return string.Equals(_value, other._value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is SetId other && Equals(other);
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
