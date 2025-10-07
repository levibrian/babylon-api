public class ApiErrorResponse
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public object[] Errors { get; set; }
}