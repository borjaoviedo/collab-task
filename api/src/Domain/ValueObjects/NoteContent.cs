using Domain.Common;

namespace Domain.ValueObjects
{
    public sealed class NoteContent : IEquatable<NoteContent>
    {
        public string Value { get; }

        private NoteContent(string value) => Value = value;

        public static NoteContent Create(string noteContent)
        {
            Guards.NotNullOrWhiteSpace(noteContent);
            noteContent = noteContent.Trim();

            Guards.LengthBetween(noteContent, 2, 500);

            return new NoteContent(noteContent);
        }

        public override string ToString() => Value;

        public bool Equals(NoteContent? other)
            => other is not null && StringComparer.OrdinalIgnoreCase.Equals(Value, other.Value);

        public override bool Equals(object? obj) => obj is NoteContent o && Equals(o);

        public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

        public static bool operator ==(NoteContent? a, NoteContent? b) => Equals(a, b);

        public static bool operator !=(NoteContent? a, NoteContent? b) => !Equals(a, b);

        public static implicit operator string(NoteContent n) => n.Value;
    }
}
