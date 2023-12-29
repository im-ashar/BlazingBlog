using BlazingBlog.Data.Models;
using BlazingBlog.Utilities;
using BlazorBootstrap;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components;
using BlazingBlog.Services;

namespace BlazingBlog.Components.Pages.Admin
{
    public partial class ManageCatergories
    {
        private Modal modal = default!;
        private Category? selectedCategory;
        private Grid<Category> grid = default!;
        [Inject] protected ToastService ToastService { get; set; } = default!;
        [Inject] protected ICategoryService CategoryService { get; set; } = default!;
        [Inject] protected NavigationManager NavigationManager { get; set; } = default!;
        private IEnumerable<Category> categories = default!;
        private async Task HandleShowOnNavbarAsync(Category category)
        {
            try
            {
                category.ShowOnNavbar = !category.ShowOnNavbar;
                await CategoryService.SaveCategoryAsync(category);
                NavigationManager.Refresh();
            }
            catch (Exception ex)
            {
                ex.ShowErrorToast(ToastService);
            }
        }
        private async Task HandleEditCategory(Category category)
        {
            try
            {
                selectedCategory = category.Clone();
                modal.Title = $"Edit {selectedCategory.Name}";
                await HandleModal(true);
            }
            catch (Exception ex)
            {
                ex.ShowErrorToast(ToastService);
            }
        }
        private async Task HandleNewCategory()
        {
            try
            {
                selectedCategory = new();
                modal.Title = "Add New Category";
                await HandleModal(true);
            }
            catch (Exception ex)
            {
                ex.ShowErrorToast(ToastService);
            }
        }
        private async Task HandleModal(bool openModal)
        {
            try
            {
                if (openModal)
                {
                    await modal.ShowAsync();
                }
                else
                {
                    selectedCategory = null;
                    await modal.HideAsync();
                }
            }
            catch (Exception ex)
            {
                ex.ShowErrorToast(ToastService);
            }
        }
        private async Task SaveCategoryAsync()
        {
            try
            {
                if (selectedCategory is not null)
                {
                    await CategoryService.SaveCategoryAsync(selectedCategory);
                    await LoadCategories();
                    await HandleModal(false);
                    await grid.RefreshDataAsync();
                    NavigationManager.Refresh();
                }
            }
            catch (Exception ex)
            {
                ex.ShowErrorToast(ToastService);
            }
        }
        private async Task LoadCategories()
        {
            try
            {
                categories = (await CategoryService.GetCategoriesAsync()).AsQueryable();
            }
            catch (Exception ex)
            {
                ex.ShowErrorToast(ToastService);
            }
        }
        private async Task DeleteCategoryAsync(Category category)
        {
            try
            {
                await CategoryService.DeleteCategoryAsync(category.Id);
                await LoadCategories();
                await grid.RefreshDataAsync();
                NavigationManager.Refresh();
            }
            catch (Exception ex)
            {
                ex.ShowErrorToast(ToastService);
            }
        }
        private async Task<GridDataProviderResult<Category>> CategoriesProvider(GridDataProviderRequest<Category> request)
        {
            if (categories is null)
            {
                await LoadCategories();
            }
            return await Task.FromResult(request.ApplyTo(categories!));
        }
    }
}
