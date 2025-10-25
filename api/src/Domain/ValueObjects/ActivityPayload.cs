using Domain.Common;
using System.Text.Json;

namespace Domain.ValueObjects
{
    public sealed class ActivityPayload : IEquatable<ActivityPayload>
    {
        public string Value { get; }

        private ActivityPayload(string value) => Value = value;

        public static ActivityPayload Create(string payload)
        {
            Guards.NotNullOrWhiteSpace(payload);

            try
            {
                JsonDocument.Parse(payload);
            }
            catch (JsonException ex)
            {
                throw new ArgumentException("Invalid JSON format.", nameof(payload), ex);
            }

            return new ActivityPayload(payload.Trim());
        }

        public override string ToString() => Value;

        public bool Equals(ActivityPayload? other)
            => other is not null && StringComparer.Ordinal.Equals(Value, other.Value);

        public override bool Equals(object? obj) => obj is ActivityPayload other && Equals(other);

        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);

        public static bool operator ==(ActivityPayload? a, ActivityPayload? b) => Equals(a, b);
        public static bool operator !=(ActivityPayload? a, ActivityPayload? b) => !Equals(a, b);

        public static implicit operator string(ActivityPayload payload) => payload.Value;
    }
}
