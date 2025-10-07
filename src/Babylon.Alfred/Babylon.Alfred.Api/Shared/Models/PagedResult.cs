namespace Babylon.Alfred.Api.Shared.Models;

public class PagedResult<T>
{
    public ApiResponse<T[]> Data { get; set; }
    public PaginationMetadata Pagination { get; set; }
}

public class PaginationMetadata
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
}
