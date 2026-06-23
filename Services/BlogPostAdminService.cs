using BlazingBlog.Data;
using BlazingBlog.Data.Models;
using BlazingBlog.Utilities;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace BlazingBlog.Services
{
    public interface IBlogPostAdminService
    {
        Task<BlogPost?> GetBlogPostByIdAsync(int id);
        Task<(BlogPost[] Items, int TotalCount)> GetBlogPostsAsync(int startIndex, int count, string? searchText = null, string? sortField = null, bool sortDescending = false, CancellationToken cancellationToken = default);
        Task<BlogPost> SaveBlogPostAsync(BlogPost blogPost, string userId);
        Task<BlogPost?> DeleteBlogPostAsync(int id);
    }

    public class BlogPostAdminService : IBlogPostAdminService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly IWebHostEnvironment _webHostEnvironment;

        private static readonly Dictionary<string, Expression<Func<BlogPost, object>>> OrderFunctions =
            new()
            {
                { nameof(BlogPost.Id), x => x.Id },
                { nameof(BlogPost.Title), x => x.Title },
                { nameof(BlogPost.Category), x => x.Category.Name },
                { nameof(BlogPost.IsPublished), x => x.IsPublished },
                { nameof(BlogPost.IsFeatured), x => x.IsFeatured }
            };

        public BlogPostAdminService(IDbContextFactory<ApplicationDbContext> contextFactory, IWebHostEnvironment webHostEnvironment)
        {
            _contextFactory = contextFactory;
            _webHostEnvironment = webHostEnvironment;
        }

        private async Task<TResult> ExecuteOnContext<TResult>(Func<ApplicationDbContext, Task<TResult>> query)
        {
            using var context = _contextFactory.CreateDbContext();
            return await query.Invoke(context);
        }

        public async Task<(BlogPost[] Items, int TotalCount)> GetBlogPostsAsync(int startIndex, int count, string? searchText = null, string? sortField = null, bool sortDescending = false, CancellationToken cancellationToken = default)
        {
            return await ExecuteOnContext(async context =>
            {
                var query = context.BlogPosts.AsNoTracking().Include(b => b.Category).AsQueryable();

                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    var search = searchText.Trim();
                    query = query.Where(b => EF.Functions.ILike(b.Title, $"%{search}%") || EF.Functions.ILike(b.Category.Name, $"%{search}%"));
                }

                var totalRecords = await query.CountAsync(cancellationToken);

                Expression<Func<BlogPost, object>> orderExpr = (!string.IsNullOrWhiteSpace(sortField) && OrderFunctions.TryGetValue(sortField, out var fn))
                    ? fn
                    : OrderFunctions[nameof(BlogPost.Id)];

                query = sortDescending ? query.OrderByDescending(orderExpr) : query.OrderBy(orderExpr);

                var items = await query.Skip(startIndex).Take(count).ToArrayAsync(cancellationToken);
                return (items, totalRecords);
            });
        }

        public async Task<BlogPost?> GetBlogPostByIdAsync(int id)
        {
            return await ExecuteOnContext(async context =>
                await context.BlogPosts.AsNoTracking().Include(b => b.Category).FirstOrDefaultAsync(b => b.Id == id));
        }

        public async Task<BlogPost> SaveBlogPostAsync(BlogPost blogPost, string userId)
        {
            return await ExecuteOnContext(async context =>
            {
                if (blogPost.Id == 0)
                {
                    await SaveNewBlogPostAsync(blogPost, userId, context);
                }
                else
                {
                    await SaveEditedBlogPostAsync(blogPost, context);
                }
                await context.SaveChangesAsync();
                return blogPost;
            });
        }

        private async Task SaveEditedBlogPostAsync(BlogPost blogPost, ApplicationDbContext context)
        {
            var isDuplicateTitle = await context.BlogPosts.AsNoTracking().AnyAsync(b => b.Title == blogPost.Title && b.Id != blogPost.Id);
            if (isDuplicateTitle)
            {
                throw new Exception($"Blog post already exists with title {blogPost.Title}");
            }
            var existingBlogPost = await context.BlogPosts.FindAsync(blogPost.Id);

            if (existingBlogPost == null)
            {
                throw new Exception($"Blog post does not exist with title {blogPost.Title}");
            }

            existingBlogPost.Title = blogPost.Title;
            existingBlogPost.Introduction = blogPost.Introduction;
            existingBlogPost.Content = blogPost.Content;
            existingBlogPost.CategoryId = blogPost.CategoryId;

            existingBlogPost.IsPublished = blogPost.IsPublished;
            existingBlogPost.IsFeatured = blogPost.IsFeatured;
            if (blogPost.IsPublished)
            {
                if (!existingBlogPost.IsPublished)
                {
                    existingBlogPost.PublishedAt = DateTime.UtcNow;
                }
            }
            else
            {
                existingBlogPost.PublishedAt = null;
            }
            await UpdateImageAsync(blogPost, existingBlogPost);
        }

        private async Task UpdateImageAsync(BlogPost blogPost, BlogPost existingBlogPost)
        {
            if (blogPost.ImageFile != null)
            {
                if (string.IsNullOrWhiteSpace(existingBlogPost.Image))
                {
                    existingBlogPost.Image = await blogPost.ImageFile.SaveFileToDiskAsync(_webHostEnvironment);
                }
                else
                {
                    var existingImagePath = Path.Combine(_webHostEnvironment.WebRootPath, existingBlogPost.Image);
                    if (File.Exists(existingImagePath))
                    {
                        File.Delete(existingImagePath);
                    }
                    existingBlogPost.Image = await blogPost.ImageFile.SaveFileToDiskAsync(_webHostEnvironment);
                }
            }
        }

        private async Task SaveNewBlogPostAsync(BlogPost blogPost, string userId, ApplicationDbContext context)
        {
            var isDuplicateTitle = await context.BlogPosts.AsNoTracking().AnyAsync(b => b.Title == blogPost.Title);
            if (isDuplicateTitle)
            {
                throw new Exception($"Blog post already exists with title {blogPost.Title}");
            }
            blogPost.Slug = await GenerateSlugAsync(blogPost);
            blogPost.CreatedAt = DateTime.UtcNow;
            blogPost.UserId = userId;
            if (blogPost.IsPublished)
            {
                blogPost.PublishedAt = DateTime.UtcNow;
            }
            blogPost.Image = await blogPost.ImageFile!.SaveFileToDiskAsync(_webHostEnvironment);
            await context.BlogPosts.AddAsync(blogPost);
        }

        private async Task<string> GenerateSlugAsync(BlogPost blogPost)
        {
            return await ExecuteOnContext(async context =>
            {
                var originalSlug = blogPost.Title.ToSlug();
                var slug = originalSlug;
                var index = 1;
                while (await context.BlogPosts.AsNoTracking().AnyAsync(b => b.Slug == slug))
                {
                    slug = $"{originalSlug}-{index++}";
                }
                return slug;
            });
        }

        public async Task<BlogPost?> DeleteBlogPostAsync(int id)
        {
            return await ExecuteOnContext(async context =>
            {
                var blogPost = await context.BlogPosts.FindAsync(id);
                if (blogPost is not null)
                {
                    context.BlogPosts.Remove(blogPost);
                    await context.SaveChangesAsync();
                }
                return blogPost;
            });
        }
    }
}
