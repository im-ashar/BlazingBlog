using BlazingBlog.Data;
using BlazingBlog.Data.Models;
using BlazingBlog.Utilities;
using BlazorBootstrap;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace BlazingBlog.Services
{
    public interface IBlogPostAdminService
    {
        Task<BlogPost?> GetBlogPostByIdAsync(int id);
        //Task<PagedResult<BlogPost>> GetBlogPostsAsync(int startIndex, int pageSize);
        Task<Tuple<IEnumerable<BlogPost>, int>> GetBlogPostsAsync(IEnumerable<FilterItem> filters, int pageNumber, int pageSize, string sortKey, SortDirection sortDirection, CancellationToken cancellationToken = default);
        Task<BlogPost> SaveBlogPostAsync(BlogPost blogPost, string userId);
    }

    public class BlogPostAdminService : IBlogPostAdminService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly IWebHostEnvironment webHostEnvironment;

        public BlogPostAdminService(IDbContextFactory<ApplicationDbContext> contextFactory, IWebHostEnvironment webHostEnvironment)
        {
            _contextFactory = contextFactory;
            this.webHostEnvironment = webHostEnvironment;
        }

        private async Task<TResult> ExecuteOnContext<TResult>(Func<ApplicationDbContext, Task<TResult>> query)
        {
            using var context = _contextFactory.CreateDbContext();
            return await query.Invoke(context);
        }

        //Server Side Pagination With MicrosoftQuickGrid

        //public async Task<PagedResult<BlogPost>> GetBlogPostsAsync(int startIndex, int pageSize)
        //{
        //    return await ExecuteOnContext(async context =>
        //    {
        //        var query = context.BlogPosts.AsNoTracking();
        //        var count = await query.CountAsync();
        //        var records = await query
        //                                .Include(b => b.Category)
        //                                .OrderByDescending(b => b.Id)
        //                                .Skip(startIndex).Take(pageSize)
        //                                .ToArrayAsync();
        //        return new PagedResult<BlogPost>(records, count);
        //    });
        //}

        //Server Side Pagination With Blazore Bootstrap
        public async Task<Tuple<IEnumerable<BlogPost>, int>> GetBlogPostsAsync(IEnumerable<FilterItem> filters, int pageNumber, int pageSize, string sortKey, SortDirection sortDirection, CancellationToken cancellationToken = default)
        {
            BlogPost[] blogPosts = Array.Empty<BlogPost>();
            using var context = _contextFactory.CreateDbContext();
            var totalRecords = await context.BlogPosts.AsNoTracking().CountAsync();
            //return await ExecuteOnContext(async context =>
            //{
            if (filters is not null && filters.Any())
            {
                var parameterExpression = Expression.Parameter(typeof(BlogPost)); // second param optional
                Expression<Func<BlogPost, bool>> lambda = null;

                foreach (var filter in filters)
                {
                    if (lambda is null)
                        lambda = ExpressionExtensions.GetExpressionDelegate<BlogPost>(parameterExpression, filter);
                    else
                        lambda = lambda.And(ExpressionExtensions.GetExpressionDelegate<BlogPost>(parameterExpression, filter));
                }
                if (sortDirection == SortDirection.Descending)
                {
                    totalRecords = context.BlogPosts.AsNoTracking().Include(b => b.Category).Where(lambda.Compile()).Count();
                    blogPosts = context.BlogPosts.AsNoTracking()
                                                 .Include(b => b.Category)
                                                 .Where(lambda.Compile())
                                                 .OrderByDescending(_ => sortKey)
                                                 .Skip((pageNumber - 1) * pageSize)
                                                 .Take(pageSize)
                                                 .ToArray();
                }
                else
                {
                    totalRecords = context.BlogPosts.AsNoTracking().Include(b => b.Category).Where(lambda.Compile()).Count();
                    blogPosts = context.BlogPosts.AsNoTracking()
                                                 .Include(b => b.Category)
                                                 .Where(lambda.Compile())
                                                 .OrderBy(_ => sortKey)
                                                 .Skip((pageNumber - 1) * pageSize)
                                                 .Take(pageSize)
                                                 .ToArray();
                }
                return new Tuple<IEnumerable<BlogPost>, int>(blogPosts, totalRecords);
            }
            else
            {
                if (sortDirection == SortDirection.Descending)
                {
                    blogPosts = await context.BlogPosts.AsNoTracking()
                                                       .Include(b => b.Category)
                                                       .OrderByDescending(_ => sortKey)
                                                       .Skip((pageNumber - 1) * pageSize)
                                                       .Take(pageSize)
                                                       .ToArrayAsync();
                }
                else
                {
                    blogPosts = await context.BlogPosts.AsNoTracking()
                                                       .Include(b => b.Category)
                                                       .OrderBy(_ => sortKey)
                                                       .Skip((pageNumber - 1) * pageSize)
                                                       .Take(pageSize)
                                                       .ToArrayAsync();
                }
                return new Tuple<IEnumerable<BlogPost>, int>(blogPosts, totalRecords);
            }

            //});
        }

        public async Task<BlogPost?> GetBlogPostByIdAsync(int id)
        {
            var category = await ExecuteOnContext(async context =>
            {
                return await context.BlogPosts.AsNoTracking().Include(b => b.Category).FirstOrDefaultAsync(b => b.Id == id);
            });
            return category;
        }
        public async Task<BlogPost> SaveBlogPostAsync(BlogPost blogPost, string userId)
        {
            var result = await ExecuteOnContext(async context =>
            {
                if (blogPost.Id == 0)
                {
                    //New Blog Post
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
                    blogPost.Image = await blogPost.ImageFile.SaveFileToDiskAsync(webHostEnvironment);
                    await context.BlogPosts.AddAsync(blogPost);
                }
                else
                {
                    //Existing Blog Post
                    var isDuplicateTitle = await context.BlogPosts.AsNoTracking().AnyAsync(b => b.Title == blogPost.Title && b.Id != blogPost.Id);
                    if (isDuplicateTitle)
                    {
                        throw new Exception($"Blog post already exists with title {blogPost.Title}");
                    }
                    var existingBlogPost = await context.BlogPosts.FindAsync(blogPost.Id);

                    existingBlogPost!.Title = blogPost.Title;

                    existingBlogPost.Image = blogPost.Image;
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
                    if (blogPost.ImageFile != null)
                    {
                        if (string.IsNullOrWhiteSpace(existingBlogPost.Image))
                        {
                            existingBlogPost.Image = await blogPost.ImageFile.SaveFileToDiskAsync(webHostEnvironment);
                        }
                        else
                        {
                            var existingImagePath = Path.Combine(webHostEnvironment.WebRootPath, existingBlogPost.Image);
                            if (File.Exists(existingImagePath))
                            {
                                File.Delete(existingImagePath);
                            }
                            existingBlogPost.Image = await blogPost.ImageFile.SaveFileToDiskAsync(webHostEnvironment);
                        }
                    }

                }
                await context.SaveChangesAsync();
                return blogPost;
            });

            return result;
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
    }
}
