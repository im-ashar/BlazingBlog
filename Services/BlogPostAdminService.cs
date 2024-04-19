using BlazingBlog.Data;
using BlazingBlog.Data.Models;
using BlazingBlog.Utilities;
using BlazorBootstrap;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Linq.Expressions;

namespace BlazingBlog.Services
{
    public interface IBlogPostAdminService
    {
        Task<BlogPost?> GetBlogPostByIdAsync(int id);
        Task<Tuple<IEnumerable<BlogPost>, int>> GetBlogPostsAsync(IEnumerable<FilterItem> filters, int pageNumber, int pageSize, string sortKey, SortDirection sortDirection, CancellationToken cancellationToken = default);
        Task<BlogPost> SaveBlogPostAsync(BlogPost blogPost, string userId);
        Task<BlogPost?> DeleteBlogPostAsync(int id);
    }

    public class BlogPostAdminService : IBlogPostAdminService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly IWebHostEnvironment _webHostEnvironment;

        private static readonly Dictionary<string, Expression<Func<BlogPost, object>>> OrderFunctions =
        new Dictionary<string, Expression<Func<BlogPost, object>>>
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

        public async Task<Tuple<IEnumerable<BlogPost>, int>> GetBlogPostsAsync(IEnumerable<FilterItem> filters, int pageNumber, int pageSize, string sortKey, SortDirection sortDirection, CancellationToken cancellationToken = default)
        {
            BlogPost[] blogPosts = Array.Empty<BlogPost>();
            return await ExecuteOnContext(async context =>
            {
                var totalRecords = await context.BlogPosts.AsNoTracking().CountAsync();
                if (filters is not null && filters.Any())
                {
                    Expression<Func<BlogPost, bool>> lambda = ApplyFilters(filters);
                    if (sortDirection == SortDirection.Descending)
                    {
                        SortByDescendingWhenFiltersAreApplied(pageNumber, pageSize, sortKey, context, out blogPosts, out totalRecords, lambda);
                    }
                    else
                    {
                        SortByAscendingWhenFiltersAreApplied(pageNumber, pageSize, sortKey, context, out blogPosts, out totalRecords, lambda);
                    }
                    return new Tuple<IEnumerable<BlogPost>, int>(blogPosts, totalRecords);
                }
                else
                {
                    if (sortDirection == SortDirection.Descending)
                    {
                        blogPosts = await SortByDescendingWhenFiltersAreNotApplied(pageNumber, pageSize, sortKey, context, blogPosts);
                    }
                    else
                    {
                        blogPosts = await SortByAscendingWhenFiltersAreNotApplied(pageNumber, pageSize, sortKey, context, blogPosts);
                    }
                    return new Tuple<IEnumerable<BlogPost>, int>(blogPosts, totalRecords);
                }

            });
        }

        private async Task<BlogPost[]> SortByAscendingWhenFiltersAreNotApplied(int pageNumber, int pageSize, string sortKey, ApplicationDbContext context, BlogPost[] blogPosts)
        {
            blogPosts = await context.BlogPosts.AsNoTracking()
                                               .Include(b => b.Category)
                                               .OrderBy(OrderFunctions[sortKey])
                                               .Skip((pageNumber - 1) * pageSize)
                                               .Take(pageSize)
                                               .ToArrayAsync();
            return blogPosts;
        }

        private async Task<BlogPost[]> SortByDescendingWhenFiltersAreNotApplied(int pageNumber, int pageSize, string sortKey, ApplicationDbContext context, BlogPost[] blogPosts)
        {
            blogPosts = await context.BlogPosts.AsNoTracking()
                                               .Include(b => b.Category)
                                               .OrderByDescending(OrderFunctions[sortKey])
                                               .Skip((pageNumber - 1) * pageSize)
                                               .Take(pageSize)
                                               .ToArrayAsync();
            return blogPosts;
        }

        private void SortByAscendingWhenFiltersAreApplied(int pageNumber, int pageSize, string sortKey, ApplicationDbContext context, out BlogPost[] blogPosts, out int totalRecords, Expression<Func<BlogPost, bool>> lambda)
        {
            var propertyInfo = typeof(BlogPost).GetProperty(sortKey);
            totalRecords = context.BlogPosts.AsNoTracking().Include(b => b.Category).Where(lambda!.Compile()).Count();
            blogPosts = context.BlogPosts.AsNoTracking()
                                         .Include(b => b.Category)
                                         .Where(lambda.Compile())
                                         .OrderBy(b => propertyInfo!.GetValue(b))
                                         .Skip((pageNumber - 1) * pageSize)
                                         .Take(pageSize)
                                         .ToArray();
        }

        private void SortByDescendingWhenFiltersAreApplied(int pageNumber, int pageSize, string sortKey, ApplicationDbContext context, out BlogPost[] blogPosts, out int totalRecords, Expression<Func<BlogPost, bool>> lambda)
        {
            var propertyInfo = typeof(BlogPost).GetProperty(sortKey);
            totalRecords = context.BlogPosts.AsNoTracking().Include(b => b.Category).Where(lambda!.Compile()).Count();
            blogPosts = context.BlogPosts.AsNoTracking()
                                         .Include(b => b.Category)
                                         .Where(lambda.Compile())
                                         .OrderByDescending(b => propertyInfo!.GetValue(b))
                                         .Skip((pageNumber - 1) * pageSize)
                                         .Take(pageSize)
                                         .ToArray();
        }

        private Expression<Func<BlogPost, bool>> ApplyFilters(IEnumerable<FilterItem> filters)
        {
            var parameterExpression = Expression.Parameter(typeof(BlogPost)); // second param optional
            Expression<Func<BlogPost, bool>> lambda = null!;

            foreach (var filter in filters)
            {
                if (lambda is null)
                    lambda = ExpressionExtensions.GetExpressionDelegate<BlogPost>(parameterExpression, filter)!;
                else
                    lambda = lambda.And(ExpressionExtensions.GetExpressionDelegate<BlogPost>(parameterExpression, filter)!);
            }

            return lambda;
        }

        public async Task<BlogPost?> GetBlogPostByIdAsync(int id)
        {
            var blogPost = await ExecuteOnContext(async context =>
            {
                return await context.BlogPosts.AsNoTracking().Include(b => b.Category).FirstOrDefaultAsync(b => b.Id == id);
            });
            return blogPost;
        }
        public async Task<BlogPost> SaveBlogPostAsync(BlogPost blogPost, string userId)
        {
            var result = await ExecuteOnContext(async context =>
            {
                if (blogPost.Id == 0)
                {
                    //New Blog Post
                    await SaveNewBlogPostAsync(blogPost, userId, context);
                }
                else
                {
                    //Existing Blog Post
                    await SaveEditedBlogPostAsync(blogPost, context);

                }
                await context.SaveChangesAsync();
                return blogPost;
            });

            return result;
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
            var newSlug = await ExecuteOnContext(async context =>
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
            return newSlug;

        }
        public async Task<BlogPost?> DeleteBlogPostAsync(int id)
        {
            var result = await ExecuteOnContext(async context =>
            {
                var blogPost = await context.BlogPosts.FindAsync(id);
                if (blogPost is not null)
                {
                    context.BlogPosts.Remove(blogPost);
                    await context.SaveChangesAsync();
                }
                return blogPost;
            });
            return result;
        }
    }
}
