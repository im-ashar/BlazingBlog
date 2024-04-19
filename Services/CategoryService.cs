using BlazingBlog.Data;
using BlazingBlog.Data.Models;
using BlazingBlog.Utilities;
using BlazorBootstrap;
using Microsoft.EntityFrameworkCore;

namespace BlazingBlog.Services
{
    public interface ICategoryService
    {
        Task DeleteCategoryAsync(int categoryId);
        Task<List<Category>> GetCategoriesAsync();
        Task<Category> GetCategoryBySlugAsync(string slug);
        Task<Category> SaveCategoryAsync(Category category);
    }

    public class CategoryService : ICategoryService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

        public CategoryService(IDbContextFactory<ApplicationDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        private async Task<TResult> ExecuteOnContext<TResult>(Func<ApplicationDbContext, Task<TResult>> query)
        {
            using var context = _contextFactory.CreateDbContext();
            return await query.Invoke(context);
        }

        public async Task<List<Category>> GetCategoriesAsync()
        {
            return await ExecuteOnContext(async context =>
            {
                return await context.Categories.AsNoTracking().ToListAsync();
            });
        }
        public async Task<Category> SaveCategoryAsync(Category category)
        {
            var result = await ExecuteOnContext(async context =>
               {
                   if (category.Id == 0)
                   {
                       if (await context.Categories.AsNoTracking().AnyAsync(c => c.Name == category.Name))
                       {
                           //Extensions.ShowError($"Category already exists with name {category.Name}");
                           throw new Exception($"Category already exists with name {category.Name}");
                       }
                       category.Slug = category.Name.ToSlug();
                       await context.Categories.AddAsync(category);
                       await context.SaveChangesAsync();
                   }
                   else
                   {
                       if (await context.Categories.AsNoTracking().AnyAsync(c => c.Name == category.Name && c.Id != category.Id))
                       {
                           //Extensions.ShowError($"Category already exists with name {category.Name}");
                           throw new Exception($"Category already exists with name {category.Name}");

                       }
                       var existingCategory = await context.Categories.FindAsync(category.Id);

                       existingCategory!.Name = category.Name;
                       existingCategory.ShowOnNavbar = category.ShowOnNavbar;

                       category.Slug = existingCategory.Slug;
                   }
                   await context.SaveChangesAsync();
                   return category;
               });
            return result!;
        }
        public async Task DeleteCategoryAsync(int categoryId)
        {
            await ExecuteOnContext(async context =>
            {
                var category = await context.Categories.FindAsync(categoryId);
                if (category == null)
                {
                    //Extensions.ShowError($"Category with id {categoryId} not found");
                    return null;
                }
                context.Categories.Remove(category);
                await context.SaveChangesAsync();
                return category;
            });
        }
        public async Task<Category> GetCategoryBySlugAsync(string slug)
        {
            var category = await ExecuteOnContext(async context =>
            {
                return await context.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Slug == slug);
            });
            if (category == null)
            {
                //Extensions.ShowError($"Category with slug {slug} not found");
                return null!;
            }
            return category;
        }
    }
}
