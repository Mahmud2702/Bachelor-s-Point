using Bachelor_s_Point.Application.Interfaces.Repositories;

namespace Bachelor_s_Point.Application.Interfaces.UnitOfWork
{
    public interface IUnitOfWork
    {
        IUserRepository UserRepo { get; }
        IRoleRepository RoleRepo { get; }
        IRoomRepository RoomRepo { get; }
        IRoomSelectionRepository SelectionRepo { get; }
        IRoomImageRepository RoomImageRepo { get; }
        IChatRepository ChatRepo { get; }
        IPendingRegistrationRepository PendingRegRepo { get; }
        Task<int> SaveAsync();
    }
}
