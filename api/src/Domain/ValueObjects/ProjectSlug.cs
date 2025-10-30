using Domain.Common;
using System.Globalization;
using System.Text;

namespace Domain.ValueObjects
{
    /// <summary>
    /// Represents a URL-friendly and normalized identifier (slug) for a project.
    /// </summary>
    public sealed class ProjectSlug : IEquatable<ProjectSlug>
    {
        public string Value { get; }

        private ProjectSlug(string value) => Value = value;

        public static ProjectSlug Create(string projectName)
        {
            Guards.NotNullOrWhiteSpace(projectName);
            var projectSlug = Normalize(projectName);

            Guards.LengthBetween(projectSlug, 1, 100);

            if (!IsAlnum(projectSlug[0]) || !IsAlnum(projectSlug[^1]))
                throw new ArgumentException("Slug must start and end with alphanumeric", nameof(projectName));

            return new ProjectSlug(projectSlug);
        }

        public override string ToString() => Value;

        public bool Equals(ProjectSlug? other) =>
            other is not null && StringComparer.Ordinal.Equals(Value, other.Value);

        public override bool Equals(object? obj) => obj is ProjectSlug o && Equals(o);

        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);

        public static bool operator ==(ProjectSlug? a, ProjectSlug? b) => Equals(a, b);

        public static bool operator !=(ProjectSlug? a, ProjectSlug? b) => !Equals(a, b);

        public static implicit operator string(ProjectSlug pSlug) => pSlug.Value;

        private static string Normalize(string input)
        {
            var s = input.Trim().ToLowerInvariant();

            s = RemoveDiacritics(s);

            // map invalid chars -> '-'; keep [a-z0-9-]
            var sb = new StringBuilder(s.Length);
            bool prevDash = false;

            foreach (var ch in s)
            {
                char mapped = ch switch
                {
                    var c when IsAlnum(c) => c,
                    '-' or ' ' or '_' => '-',
                    _ => '-'
                };

                if (mapped == '-')
                {
                    if (!prevDash)
                    {
                        sb.Append('-');
                        prevDash = true;
                    }
                }
                else
                {
                    sb.Append(mapped);
                    prevDash = false;
                }
            }

            // trim leading/trailing dashes
            var result = sb.ToString().Trim('-');

            return result;
        }

        private static string RemoveDiacritics(string s)
        {
            var norm = s.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(capacity: norm.Length);
            foreach (var ch in norm)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (uc != UnicodeCategory.NonSpacingMark) sb.Append(ch);
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        private static bool IsAlnum(char c) => (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9');
    }
}
