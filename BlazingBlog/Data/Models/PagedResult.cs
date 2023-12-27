namespace BlazingBlog.Data.Models
{
    public record PagedResult<TResult>(TResult[] Records, int TotalCount);
}
