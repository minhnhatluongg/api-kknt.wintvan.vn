namespace api.kknt.Domain.Entities.Dashboard
{
    /// <summary>
    /// Một tin tức.
    /// </summary>
    public class NewsItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Image { get; set; }
        public string? Body_Cut { get; set; }
    }

    /// <summary>
    /// Tin tức nổi bật + danh sách tin tức.
    /// </summary>
    public class ListNews
    {
        public NewsItem News { get; set; } = new();
        public List<NewsItem> Lstnews { get; set; } = new();
    }
}
