
namespace Domain.ValueObjects
{
    public sealed class NoteContent : IEquatable<NoteContent>
    {
        public string Value { get; }

        private NoteContent(string value) => Value = value;

        public static NoteContent Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Note content cannot be empty.", nameof(value));

            value = value.Trim();

            if (value.Length < 2 || value.Length > 500)
                throw new ArgumentException("Note content must be between 2 and 2000 characters", nameof(value));

            return new NoteContent(value);
        }

        public override string ToString() => Value;

        public bool Equals(NoteContent? other) =>
            other is not null && StringComparer.OrdinalIgnoreCase.Equals(Value, other.Value);

        public override bool Equals(object? obj) => obj is NoteContent o && Equals(o);

        public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

        public static bool operator ==(NoteContent? a, NoteContent? b) => Equals(a, b);

        public static bool operator !=(NoteContent? a, NoteContent? b) => !Equals(a, b);

        public static implicit operator string(NoteContent n) => n.Value;
    }
}
