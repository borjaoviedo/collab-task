
namespace Application.Common.Results
{
    public readonly record struct WriteResult(DataWriteConflict Conflict, byte[]? NewRowVersion)
    {
        public bool IsSuccess => Conflict == DataWriteConflict.None;
    }
}
