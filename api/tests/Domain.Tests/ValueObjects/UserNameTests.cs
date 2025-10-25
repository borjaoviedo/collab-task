using Domain.ValueObjects;
using FluentAssertions;

namespace Domain.Tests.ValueObjects
{
    public class UserNameTests
    {
        private readonly string _defaultUserName = "user name";

        [Fact]
        public void Create_ValidName_ReturnsInstance()
        {
            var userName = UserName.Create(_defaultUserName);

            userName.Value.Should().Be(_defaultUserName);
        }

        [Theory]
        [InlineData("José")]
        [InlineData("Jòse")]
        public void Create_Accented_Letters_Pass_With_Unicode_Regex(string input)
            => UserName.Create(input).Value.Should().Be(input);

        [Fact]
        public void Create_MinLength_Passes()
            => UserName.Create("aa").Value.Should().HaveLength(2);

        [Fact]
        public void Create_MaxLength100_Passes()
            => UserName.Create(new string('x', 100)).Value.Should().HaveLength(100);

        [Theory]
        [InlineData(" ")]
        [InlineData("   Not Trimmed   ")]
        [InlineData("   Second Not Trimmed")]
        [InlineData("Third Not Trimmed    ")]
        public void Create_Trim_Applied_Correctly(string input)
        {
            var trimmedInput = input.Trim();

            if (trimmedInput.Length == 0)
                Assert.Throws<ArgumentException>(() => UserName.Create(input));
            else
                UserName.Create(input).Value.Should().Be(trimmedInput);
        }

        [Theory]
        [InlineData("John!")]
        [InlineData("John Do3")]
        [InlineData("John Doe 1")]
        [InlineData("John_Doe")]
        [InlineData("John-Doe")]
        [InlineData("John@Doe")]
        [InlineData("John+Doe")]
        public void Create_InvalidFormat_Throws(string input)
            => Assert.Throws<ArgumentException>(() => UserName.Create(input));

        [Theory]
        [InlineData("John  Doe")]
        [InlineData("John   Doe")]
        [InlineData("John    Doe")]
        [InlineData("John Doe  Junior")]
        [InlineData("John Doe   Junior")]
        [InlineData("John  Doe  Senior")]
        public void Create_Name_With_Two_Or_More_Consecutive_Spaces_Throws(string input)
            => Assert.Throws<ArgumentException>(() => UserName.Create(input));

        [Fact]
        public void Create_TooLongName_Throws()
        {
            var random = new Random();
            var chars = Enumerable.Range(0, 101)
                .Select(_ => (char)random.Next('a', 'z' + 1))
                .ToArray();
            var tooLongName = new string(chars);

            Assert.Throws<ArgumentOutOfRangeException>(() => UserName.Create(tooLongName));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_NullEmptyOrWhitespace_Throws(string input)
            => Assert.Throws<ArgumentException>(() => UserName.Create(input));


        [Fact]
        public void ToString_ReturnsValue()
        {
            var userName = UserName.Create(_defaultUserName);

            userName.ToString().Should().Be(_defaultUserName);
        }

        [Fact]
        public void Equality_SameValue_True()
        {
            var userNameA = UserName.Create(_defaultUserName);
            var userNameB = UserName.Create(_defaultUserName);

            userNameA.Equals(userNameB).Should().BeTrue();
        }

        [Fact]
        public void Equality_DifferentValue_False()
        {
            var userNameA = UserName.Create(_defaultUserName);
            var userNameB = UserName.Create("different user name");

            userNameA.Equals(userNameB).Should().BeFalse();
        }

        [Fact]
        public void Equality_IgnoresCase()
        {
            var userNameA = UserName.Create("User Name");
            var userNameB = UserName.Create("user name");

            userNameA.Should().Be(userNameB);
            userNameA.GetHashCode().Should().Be(userNameB.GetHashCode());
        }

        [Fact]
        public void Equality_Is_Symmetric_And_Transitive()
        {
            var userNameA = UserName.Create("Alice Smith");
            var userNameB = UserName.Create("alice smith");
            var userNameC = UserName.Create("ALICE SMITH");

            userNameA.Equals(userNameB).Should().BeTrue();
            userNameB.Equals(userNameA).Should().BeTrue();
            userNameA.Equals(userNameB).Should().BeTrue();
            userNameB.Equals(userNameC).Should().BeTrue();
            userNameA.Equals(userNameC).Should().BeTrue();
        }

        [Fact]
        public void Operators_Equality_SameValue_True()
        {
            var userNameA = UserName.Create(_defaultUserName);
            var userNameB = UserName.Create(_defaultUserName);

            (userNameA == userNameB).Should().BeTrue();
            userNameA.GetHashCode().Should().Be(userNameB.GetHashCode());
        }

        [Fact]
        public void Operators_Equality_DifferentValue_False()
        {
            var userNameA = UserName.Create(_defaultUserName);
            var userNameB = UserName.Create("different user name");

            (userNameA == userNameB).Should().BeFalse();
        }

        [Fact]
        public void Operators_Inequality_SameValue_False()
        {
            var userNameA = UserName.Create(_defaultUserName);
            var userNameB = UserName.Create(_defaultUserName);

            (userNameA != userNameB).Should().BeFalse();
        }

        [Fact]
        public void Operators_Inequality_DifferentValue_True()
        {
            var userNameA = UserName.Create(_defaultUserName);
            var userNameB = UserName.Create("different user name");

            (userNameA != userNameB).Should().BeTrue();
        }

        [Fact]
        public void Operators_Handle_Nulls()
        {
            UserName? userNameA = null;
            UserName? userNameB = null;
            var userNameC = UserName.Create(_defaultUserName);

            (userNameA == userNameB).Should().BeTrue();
            (userNameA == userNameC).Should().BeFalse();
            (userNameC != null).Should().BeTrue();
        }

        [Fact]
        public void Implicit_ToString_Works()
        {
            UserName userName = UserName.Create(_defaultUserName);
            string str = userName;

            str.Should().Be(_defaultUserName);
        }
    }
}
