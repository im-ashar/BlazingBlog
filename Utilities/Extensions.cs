using BlazorBootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace BlazingBlog.Utilities
{
    public static class Extensions
    {
        public static string GetFullNameOfUser(this ClaimsPrincipal principal) => principal.FindFirstValue(AppConstants.ClaimNames.FullName)!;
        public static string GetIdOfUser(this ClaimsPrincipal principal) => principal.FindFirstValue(ClaimTypes.NameIdentifier)!;

        public static string ToSlug(this string name)
        {
            name = Regex.Replace(name.ToLowerInvariant(), @"[^a-z0-9_]", "-");
            name = name.Replace("--", "-")
                      .Trim('-');
            return name;
        }
        public static void ShowErrorToast(this Exception ex, ToastService toastService)
        {
            toastService.Notify(new ToastMessage
            {
                Title = "Error",
                Message = ex.Message,
                Type = ToastType.Danger
            });
        }
        public static void ShowSuccessToast(this ToastService toastService, string message)
        {
            toastService.Notify(new ToastMessage
            {
                Title = "Success",
                Message = message,
                Type = ToastType.Success
            });
        }
        public static async Task<string> SaveFileToDiskAsync(this IBrowserFile fileToBeUploaded, IWebHostEnvironment webHostEnvironment)
        {
            var randomFileName = Path.GetRandomFileName();
            var extension = Path.GetExtension(fileToBeUploaded.Name);

            var folderPath = Path.Combine(webHostEnvironment.WebRootPath, "images", "posts");
            Directory.CreateDirectory(folderPath);

            var filePath = Path.Combine("images", "posts", randomFileName + extension);

            var absolutePath = Path.Combine(webHostEnvironment.WebRootPath, filePath);
            await using var stream = new FileStream(absolutePath, FileMode.Create);
            try
            {
                var maxAllowedSize = AppConstants.ImageSettings.MaxImageSize;
                await fileToBeUploaded.OpenReadStream(maxAllowedSize).CopyToAsync(stream);
                return filePath.Replace('\\', '/'); ;
            }
            catch
            {
                stream.Close();
                throw new Exception($"Error While Uploading Image");
            }
        }
        public static string ToDisplay(this DateTime? dateTime) => dateTime?.ToString("MMM dd") ?? string.Empty;
    }
}
