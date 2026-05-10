using System.Linq.Expressions;

namespace Bachelor_s_Point.Application.Interfaces.Repositories
{
    public interface IBaseRepository<T> where T : class
    {
        Task<List<T>> GetAllAsync();

        Task<T?> GetByIdAsync(int id);

        Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate);

        Task AddAsync(T entity);

        void Update(T entity);

        void Delete(T entity);
    }
}
