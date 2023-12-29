using BlazingBlog.Data;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore;
using BlazingBlog.Data.Models;

namespace BlazingBlog.Services
{
    public interface ISubscriberService
    {
        Task<Subscriber> SaveSubscriberAsync(Subscriber subscriber);
        Task<PagedResult<Subscriber>> GetSubscribersAsync(int pageIndex, int pageSize);
    }

    public class SubscriberService : ISubscriberService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

        public SubscriberService(IDbContextFactory<ApplicationDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        private async Task<TResult> ExecuteOnContext<TResult>(Func<ApplicationDbContext, Task<TResult>> query)
        {
            using var context = _contextFactory.CreateDbContext();
            return await query.Invoke(context);
        }

        //Save Subscriber
        public async Task<Subscriber> SaveSubscriberAsync(Subscriber subscriber)
        {
            return await ExecuteOnContext(async context =>
            {
                var existingSubscriber = await context.Subscribers.FirstOrDefaultAsync(s => s.Email == subscriber.Email);
                if (existingSubscriber != null)
                {
                    throw new Exception("Subscriber already exists");
                }
                else
                {
                    subscriber.SubscribedOn = DateTime.UtcNow;
                    context.Subscribers.Add(subscriber);
                    await context.SaveChangesAsync();
                    return subscriber;
                }
            });
        }

        //Get Subscribers Using Pagination Server Side Microsoft Grid
        public async Task<PagedResult<Subscriber>> GetSubscribersAsync(int pageIndex, int pageSize)
        {
            return await ExecuteOnContext(async context =>
            {
                var query = context.Subscribers.AsNoTracking();
                var count = await query.CountAsync();
                var records = await query
                                        .OrderByDescending(s => s.SubscribedOn)
                                        .Skip(pageIndex)
                                        .Take(pageSize)
                                        .ToArrayAsync();
                return new PagedResult<Subscriber>(records, count);
            });
        }
    }
}
