using BlazingBlog.Data;
using BlazingBlog.Data.Models;
using BlazingBlog.Utilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BlazingBlog.Services
{
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
			if (await _userManager.FindByNameAsync(AppConstants.AdminAccount.Username) == null)
			{
				// if it doesn't exist, create it
				var user = new ApplicationUser
				{
					UserName = AppConstants.AdminAccount.Username,
					Email = AppConstants.AdminAccount.Email,
					FullName = AppConstants.AdminAccount.Name,
					EmailConfirmed = true,
					LockoutEnabled = false
				};
				var result = await _userManager.CreateAsync(user, AppConstants.AdminAccount.Password);
				if (!result.Succeeded)
				{
					var errors = result.Errors.Select(e => e.Description);
					throw new Exception(string.Join(Environment.NewLine, errors));
				}
				//Add Admin user to AdminRole
				result = await _userManager.AddToRoleAsync(user, AppConstants.RolesTypes.AdminRole);
				if (!result.Succeeded)
				{
					var errors = result.Errors.Select(e => e.Description);
					throw new Exception(string.Join(Environment.NewLine, errors));
				}
			}
		}

		private async Task SeedUserRoleIfNotExists()
		{
			if (!await _roleManager.RoleExistsAsync(AppConstants.RolesTypes.UserRole))
			{
				// if it doesn't exist, create it
				var result = await _roleManager.CreateAsync(new IdentityRole(AppConstants.RolesTypes.UserRole));
				if (!result.Succeeded)
				{
					var errors = result.Errors.Select(e => e.Description);
					throw new Exception(string.Join(Environment.NewLine, errors));
				}
			}
		}

		private async Task SeedAdminRoleIfNotExists()
		{
			if (!await _roleManager.RoleExistsAsync(AppConstants.RolesTypes.AdminRole))
			{
				// if it doesn't exist, create it
				var result = await _roleManager.CreateAsync(new IdentityRole(AppConstants.RolesTypes.AdminRole));
				if (!result.Succeeded)
				{
					var errors = result.Errors.Select(e => e.Description);
					throw new Exception(string.Join(Environment.NewLine, errors));
				}
			}
		}
	}
}
