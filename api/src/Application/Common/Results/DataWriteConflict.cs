
namespace Application.Common.Results
{
    public enum DataWriteConflict
    {
        None = 0,
        NotFound = 1,
        Concurrency = 2
    }
}
