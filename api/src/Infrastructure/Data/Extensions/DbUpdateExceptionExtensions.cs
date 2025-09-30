using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data.Extensions
{
    public static class DbUpdateExceptionExtensions
    {
        public static bool IsUniqueViolation(this DbUpdateException ex, string? indexName = null)
        {
            var baseEx = ex.GetBaseException();

            if (baseEx is SqlException sql && (sql.Number == 2601 || sql.Number == 2627))
            {
                if (string.IsNullOrWhiteSpace(indexName))
                    return true;

                var msg = ex.InnerException?.Message ?? baseEx.Message;
                return msg?.IndexOf(indexName, StringComparison.OrdinalIgnoreCase) >= 0;
            }

            return false;
        }
    }
}
