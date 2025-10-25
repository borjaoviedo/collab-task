using Domain.ValueObjects;
using FluentAssertions;
using System.Text.Json;

namespace Domain.Tests.ValueObjects
{
    public sealed class ActivityPayloadTests
    {
        private readonly string _defaultPayload = "{ \"a\": 1 }";

        [Fact]
        public void Create_WithValidJson_ReturnsValueObject()
        {
            var payload = ActivityPayload.Create(_defaultPayload);

            payload.Should().NotBeNull();
            payload.Value.Should().Be(_defaultPayload);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_EmptyOrWhitespace_Throws(string input)
        {
            var act = () => ActivityPayload.Create(input);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Create_InvalidJson_Throws()
        {
            var invalidJson = "{ a: 1 ";
            var act = () => ActivityPayload.Create(invalidJson);

            act.Should().Throw<ArgumentException>().WithInnerException<JsonException>();
        }

        [Fact]
        public void Equality_SameString_IsEqual()
        {
            var activityPayloadA = ActivityPayload.Create(_defaultPayload);
            var activityPayloadB = ActivityPayload.Create(_defaultPayload);

            activityPayloadA.Should().Be(activityPayloadB);
            (activityPayloadA == activityPayloadB).Should().BeTrue();
            activityPayloadA.Equals(activityPayloadB).Should().BeTrue();
            activityPayloadA.GetHashCode().Should().Be(activityPayloadB.GetHashCode());
        }

        [Fact]
        public void Equality_DifferentString_IsNotEqual()
        {
            var activityPayloadA = ActivityPayload.Create(_defaultPayload);
            var activityPayloadB = ActivityPayload.Create("{\"x\":3}");

            activityPayloadA.Should().NotBe(activityPayloadB);
            (activityPayloadA != activityPayloadB).Should().BeTrue();
        }

        [Fact]
        public void Implicit_ToString_Works()
        {
            var activityPayload = ActivityPayload.Create(_defaultPayload);
            string activityPayloadString = activityPayload;

            activityPayloadString.Should().Be(_defaultPayload);
            activityPayload.ToString().Should().Be(activityPayloadString);
        }
    }
}
