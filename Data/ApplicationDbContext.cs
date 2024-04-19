using BlazingBlog.Data.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BlazingBlog.Data
{
	public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
	{
		public DbSet<BlogPost> BlogPosts { get; set; }
		public DbSet<Category> Categories { get; set; }
		public DbSet<Subscriber> Subscribers { get; set; }
	}
}