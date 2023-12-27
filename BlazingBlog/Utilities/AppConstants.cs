namespace BlazingBlog.Utilities
{
    public static class AppConstants
    {
        public static class ImageSettings
        {
            public const int MaxImageSize = 1024 * 1024;
            public static string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
        }
        public static class ClaimNames
        {
            public const string Name = "Name";
        }
        public static class AdminAccount
        {
            public const string Username = "admin@email.com";
            public const string Password = "P@ssword1";
            public const string Email = "admin@email.com";
            public const string Name = "Administrator";
        }
        public static class RolesTypes
        {
            public const string AdminRole = "Admin";
            public const string UserRole = "User";
        }
    }
}
