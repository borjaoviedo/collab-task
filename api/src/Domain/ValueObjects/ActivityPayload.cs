using System.Text.Json;

namespace Domain.ValueObjects
{
    public sealed class ActivityPayload
    {
        public string Value { get; }

        private ActivityPayload(string value) => Value = value;

        public static ActivityPayload Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Payload cannot be empty.", nameof(value));

            try
            {
                JsonDocument.Parse(value);
            }
            catch (JsonException ex)
            {
                throw new ArgumentException("Invalid JSON format.", nameof(value), ex);
            }

            return new ActivityPayload(value.Trim());
        }

        public override string ToString() => Value;

        public bool Equals(ActivityPayload? other) =>
            other is not null && StringComparer.Ordinal.Equals(Value, other.Value);

        public override bool Equals(object? obj) => obj is ActivityPayload other && Equals(other);

        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);

        public static bool operator ==(ActivityPayload? a, ActivityPayload? b) => Equals(a, b);
        public static bool operator !=(ActivityPayload? a, ActivityPayload? b) => !Equals(a, b);

        public static implicit operator string(ActivityPayload payload) => payload.Value;
    }
}
