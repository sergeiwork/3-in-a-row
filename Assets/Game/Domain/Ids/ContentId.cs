using System;

namespace ThreeInARow.Domain.Ids
{
    /// <summary>Stable, serialized content key. Never use Unity asset instance IDs in save data.</summary>
    [Serializable]
    public readonly struct ContentId : IEquatable<ContentId>, IComparable<ContentId>
    {
        public readonly string Value;

        public ContentId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("A content ID cannot be empty.", nameof(value));
            }

            Value = value;
        }

        public bool Equals(ContentId other) => StringComparer.Ordinal.Equals(Value, other.Value);
        public override bool Equals(object obj) => obj is ContentId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
        public int CompareTo(ContentId other) => StringComparer.Ordinal.Compare(Value, other.Value);
        public override string ToString() => Value;
        public static implicit operator ContentId(string value) => new ContentId(value);
    }
}
