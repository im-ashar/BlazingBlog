using BlazingBlog.Data.Models;
using BlazingBlog.Services;
using BlazingBlog.Utilities;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BlazingBlog.Components.Pages.Admin
{
    public partial class ManageCatergories
    {
        private Category? selectedCategory;
        private List<Category> categories = new();

        [Inject] protected ToastService ToastService { get; set; } = default!;
        [Inject] protected ICategoryService CategoryService { get; set; } = default!;
        [Inject] protected NavigationManager NavigationManager { get; set; } = default!;
        [Inject] protected IJSRuntime JS { get; set; } = default!;

        protected override async Task OnInitializedAsync()
        {
            await LoadCategories();
        }

        private async Task LoadCategories()
        {
            try
            {
                categories = await CategoryService.GetCategoriesAsync();
            }
            catch (Exception ex)
            {
                ex.ShowErrorToast(ToastService);
            }
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
                _modalTitle = $"Edit {selectedCategory.Name}";
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
                selectedCategory = new Category();
                _modalTitle = "New category";
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
                    await JS.InvokeVoidAsync("BlazingBlogModal.show", "category-modal");
                }
                else
                {
                    await JS.InvokeVoidAsync("BlazingBlogModal.close", "category-modal");
                    selectedCategory = null;
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
                    ToastService.ShowSuccessToast("Category saved.");
                    NavigationManager.Refresh();
                }
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
                ToastService.ShowSuccessToast("Category deleted.");
                NavigationManager.Refresh();
            }
            catch (Exception ex)
            {
                ex.ShowErrorToast(ToastService);
            }
        }
    }
}
