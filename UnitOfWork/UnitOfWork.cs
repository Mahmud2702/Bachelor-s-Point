using Bachelor_s_Point.Application.Interfaces.Repositories;
using Bachelor_s_Point.Application.Interfaces.UnitOfWork;
using Bachelor_s_Point.Data;

namespace Bachelor_s_Point.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;

        public IUserRepository UserRepo { get; private set; }

        public IRoleRepository RoleRepo { get; private set; }

        public IRoomRepository RoomRepo { get; private set; }

        public IRoomSelectionRepository SelectionRepo { get; private set; }

        public UnitOfWork(
            AppDbContext context,
            IUserRepository userRepo,
            IRoleRepository roleRepo,
            IRoomRepository roomRepo,
            IRoomSelectionRepository selectionRepo)
        {
            _context = context;
            UserRepo = userRepo;
            RoleRepo = roleRepo;
            RoomRepo = roomRepo;
            SelectionRepo = selectionRepo;
        }

        public async Task<int> SaveAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}
