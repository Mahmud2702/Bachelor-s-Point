using Bachelor_s_Point.Models;

namespace Bachelor_s_Point.Application.Interfaces.Repositories
{
    public interface ILoginHistoryRepository : IBaseRepository<LoginHistory>
    {
        Task<List<LoginHistory>> GetRecentAsync(int take = 200);
        Task<int> GetTotalLoginCountAsync();
        Task<int> GetDistinctUserCountAsync();
        Task<int> GetTodayLoginCountAsync();
    }
}
