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
        private Modal modal = new Modal();
        private Category? selectedCategory;
        [Inject] protected ToastServiceExtended ToastService { get; set; }
        [Inject] protected ICategoryService CategoryService { get; set; }
        [Inject] protected NavigationManager NavigationManager { get; set; }
        private IEnumerable<Category> categories = default!;
        protected override async Task OnInitializedAsync()
        {
            await LoadCategories();
        }
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
            return await Task.FromResult(request.ApplyTo(categories));
        }
    }
}
