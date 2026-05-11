using Bachelor_s_Point.Application.Interfaces.Repositories;
using Bachelor_s_Point.Data;
using Bachelor_s_Point.Models;

namespace Bachelor_s_Point.Repositories
{
    public class RoomImageRepository : BaseRepository<RoomImage>, IRoomImageRepository
    {
        public RoomImageRepository(AppDbContext context) : base(context) { }
    }
}
