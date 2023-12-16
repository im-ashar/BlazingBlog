using BlazingBlog.Data;
using BlazingBlog.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BlazingBlog.Services
{
	internal class AdminAccount
	{
		public const string Username = "admin";
		public const string Password = "P@ssword1";
		public const string Email = "admin@email.com";
		public const string Name = "Administrator";
		public const string AdminRole = "Admin";
		public const string UserRole = "User";
	}

	public interface ISeedDataService
	{
		Task SeedDataAsync();
	}

	public class SeedDataService : ISeedDataService
	{
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly RoleManager<IdentityRole> _roleManager;
		private readonly ApplicationDbContext _context;
		public SeedDataService(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, ApplicationDbContext context)
		{
			_roleManager = roleManager;
			_userManager = userManager;
			_context = context;
		}
		public async Task SeedDataAsync()
		{
			await MigrateDatabase();
			await SeedAdminRoleIfNotExists();
			await SeedUserRoleIfNotExists();
			await SeedAdminUserIfNotExists();
			await SeedCategoriesIfNotExists();
		}

		private async Task MigrateDatabase()
		{
#if DEBUG
			await _context.Database.MigrateAsync();
#endif
		}

		private async Task SeedCategoriesIfNotExists()
		{
			if (!(await _context.Categories.AnyAsync()))
			{
				var categories = Category.GetSeedCatergories();
				await _context.Categories.AddRangeAsync(categories);
				await _context.SaveChangesAsync();
			}
		}

		private async Task SeedAdminUserIfNotExists()
		{
			if (await _userManager.FindByNameAsync(AdminAccount.Username) == null)
			{
				// if it doesn't exist, create it
				var user = new ApplicationUser
				{
					UserName = AdminAccount.Username,
					Email = AdminAccount.Email,
					Name = AdminAccount.Name
				};
				var result = await _userManager.CreateAsync(user, AdminAccount.Password);
				if (!result.Succeeded)
				{
					var errors = result.Errors.Select(e => e.Description);
					throw new Exception(string.Join(Environment.NewLine, errors));
				}
				//Add Admin user to AdminRole
				result = await _userManager.AddToRoleAsync(user, AdminAccount.AdminRole);
				if (!result.Succeeded)
				{
					var errors = result.Errors.Select(e => e.Description);
					throw new Exception(string.Join(Environment.NewLine, errors));
				}
			}
		}

		private async Task SeedUserRoleIfNotExists()
		{
			if (!await _roleManager.RoleExistsAsync(AdminAccount.UserRole))
			{
				// if it doesn't exist, create it
				var result = await _roleManager.CreateAsync(new IdentityRole(AdminAccount.UserRole));
				if (!result.Succeeded)
				{
					var errors = result.Errors.Select(e => e.Description);
					throw new Exception(string.Join(Environment.NewLine, errors));
				}
			}
		}

		private async Task SeedAdminRoleIfNotExists()
		{
			if (!await _roleManager.RoleExistsAsync(AdminAccount.AdminRole))
			{
				// if it doesn't exist, create it
				var result = await _roleManager.CreateAsync(new IdentityRole(AdminAccount.AdminRole));
				if (!result.Succeeded)
				{
					var errors = result.Errors.Select(e => e.Description);
					throw new Exception(string.Join(Environment.NewLine, errors));
				}
			}
		}
	}
}
