namespace BlazingBlog.Data.Models
{
    public record DetailPageModel(BlogPost? BlogPost, BlogPost[] RelatedPosts)
    {
        public static DetailPageModel Empty() => new(null, []);
        public bool IsEmpty => BlogPost is null;
    }
}
