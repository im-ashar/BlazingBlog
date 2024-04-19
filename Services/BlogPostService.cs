using BlazingBlog.Data;
using BlazingBlog.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace BlazingBlog.Services
{
    public interface IBlogPostService
    {
        Task<DetailPageModel> GetBlogPostBySlugAsync(string slug);
        Task<BlogPost[]> GetFeaturedBlogPostsAsync(int count, int categoryId = 0);
        Task<BlogPost[]> GetPopularBlogPostsAsync(int count, int categoryId = 0);
        Task<BlogPost[]> GetRecentBlogPostsAsync(int count, int categoryId = 0);
        Task<BlogPost[]> GetBlogPostsAsync(int pageIndex, int pageSize, int categoryId = 0);
    }

    public class BlogPostService : IBlogPostService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

        public BlogPostService(IDbContextFactory<ApplicationDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        private async Task<TResult> ExecuteOnContext<TResult>(Func<ApplicationDbContext, Task<TResult>> query)
        {
            using var context = _contextFactory.CreateDbContext();
            return await query.Invoke(context);
        }
        public async Task<BlogPost[]> GetFeaturedBlogPostsAsync(int count, int categoryId = 0)
        {
            return await ExecuteOnContext(async context =>
            {
                var query = context.BlogPosts.AsNoTracking()
                                            .Include(b => b.Category)
                                            .Include(b => b.User)
                                            .Where(b => b.IsPublished);
                if (categoryId > 0)
                {
                    query = query.Where(b => b.CategoryId == categoryId);
                }

                var records = await query.Where(b => b.IsFeatured)
                                   .OrderBy(_ => Guid.NewGuid())
                                   .Take(count)
                                   .ToArrayAsync();
                if (records.Length < count)
                {
                    var additionalRecords = await query.Where(b => !b.IsFeatured)
                                                       .OrderBy(_ => Guid.NewGuid())
                                                       .Take(count - records.Length)
                                                       .ToArrayAsync();
                    records = [.. records, .. additionalRecords];
                }
                return records;
            });
        }
        public async Task<BlogPost[]> GetPopularBlogPostsAsync(int count, int categoryId = 0)
        {
            return await ExecuteOnContext(async context =>
            {
                var query = context.BlogPosts.AsNoTracking()
                                            .Include(p => p.Category)
                                            .Include(b => b.User)
                                            .Where(b => b.IsPublished);
                if (categoryId > 0)
                {
                    query = query.Where(b => b.CategoryId == categoryId);
                }
                return await query.OrderByDescending(b => b.ViewCount)
                                  .Take(count)
                                  .ToArrayAsync();
            });
        }
        public async Task<BlogPost[]> GetRecentBlogPostsAsync(int count, int categoryId = 0) =>
            await GetPostsAsync(0, count, categoryId);

        public async Task<BlogPost[]> GetBlogPostsAsync(int pageIndex, int pageSize, int categoryId = 0) =>
            await GetPostsAsync((pageIndex - 1) * pageSize, pageSize, categoryId);

        public async Task<DetailPageModel> GetBlogPostBySlugAsync(string slug)
        {
            return await ExecuteOnContext(async context =>
            {
                var blogPost = await context.BlogPosts.AsNoTracking()
                                                      .Include(p => p.Category)
                                                      .Include(b => b.User)
                                                      .FirstOrDefaultAsync(b => b.Slug == slug && b.IsPublished);
                if (blogPost == null)
                {
                    DetailPageModel.Empty();
                }
                var relatedPosts = await context.BlogPosts.AsNoTracking()
                                                          .Include(p => p.Category)
                                                          .Include(b => b.User)
                                                          .Where(b => b.IsPublished && b.CategoryId == blogPost!.CategoryId && b.Id != blogPost.Id)
                                                          .OrderByDescending(b => b.PublishedAt)
                                                          .Take(4)
                                                          .ToArrayAsync();
                return new DetailPageModel(blogPost, relatedPosts);
            });
        }
        private async Task<BlogPost[]> GetPostsAsync(int skip, int take, int categoryId)
        {
            return await ExecuteOnContext(async context =>
            {
                var query = context.BlogPosts.AsNoTracking()
                                            .Include(p => p.Category)
                                            .Include(b => b.User)
                                            .Where(b => b.IsPublished);
                if (categoryId > 0)
                {
                    query = query.Where(b => b.CategoryId == categoryId);
                }
                return await query.OrderByDescending(b => b.PublishedAt)
                                  .Skip(skip)
                                  .Take(take)
                                  .ToArrayAsync();
            });
        }
    }
}
