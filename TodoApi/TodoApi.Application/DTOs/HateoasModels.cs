namespace TodoApi.Application.DTOs
{
    public class Link
    {
        public string Rel { get; set; } = string.Empty;
        public string Href { get; set; } = string.Empty;
        public string Method { get; set; } = "GET";
        public string? Type { get; set; }
    }

    public class HateoasResponse<T>
    {
        public T Data { get; set; } = default!;
        public List<Link> Links { get; set; } = new List<Link>();
    }
}