using Bachelor_s_Point.Application.Interfaces.Repositories;
using Bachelor_s_Point.Application.Interfaces.UnitOfWork;
using Bachelor_s_Point.Data;

namespace Bachelor_s_Point.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;

        public IUserRepository UserRepo { get; }
        public IRoleRepository RoleRepo { get; }
        public IRoomRepository RoomRepo { get; }
        public IRoomSelectionRepository SelectionRepo { get; }
        public IRoomImageRepository RoomImageRepo { get; }
        public IChatRepository ChatRepo { get; }
        public IPendingRegistrationRepository PendingRegRepo { get; }
        public IPasswordResetRepository PasswordResetRepo { get; }
        public IKycRepository KycRepo { get; }
        public ILoginHistoryRepository LoginHistoryRepo { get; }

        public UnitOfWork(
            AppDbContext context,
            IUserRepository userRepo,
            IRoleRepository roleRepo,
            IRoomRepository roomRepo,
            IRoomSelectionRepository selectionRepo,
            IRoomImageRepository roomImageRepo,
            IChatRepository chatRepo,
            IPendingRegistrationRepository pendingRegRepo,
            IPasswordResetRepository passwordResetRepo,
            IKycRepository kycRepo,
            ILoginHistoryRepository loginHistoryRepo)
        {
            _context = context;
            UserRepo = userRepo;
            RoleRepo = roleRepo;
            RoomRepo = roomRepo;
            SelectionRepo = selectionRepo;
            RoomImageRepo = roomImageRepo;
            ChatRepo = chatRepo;
            PendingRegRepo = pendingRegRepo;
            PasswordResetRepo = passwordResetRepo;
            KycRepo = kycRepo;
            LoginHistoryRepo = loginHistoryRepo;
        }

        public async Task<int> SaveAsync() => await _context.SaveChangesAsync();
    }
}
