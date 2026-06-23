using Bachelor_s_Point.Application.Interfaces.Repositories;

namespace Bachelor_s_Point.Application.Interfaces.UnitOfWork
{
    public interface IUnitOfWork
    {
        IUserRepository               UserRepo        { get; }
        IRoleRepository               RoleRepo        { get; }
        IAdminRepository              AdminRepo       { get; }
        IRoomRepository               RoomRepo        { get; }
        IRoomSelectionRepository      SelectionRepo   { get; }
        IRoomImageRepository          RoomImageRepo   { get; }
        IChatRepository               ChatRepo        { get; }
        IPendingRegistrationRepository PendingRegRepo  { get; }
        IPasswordResetRepository      PasswordResetRepo { get; }
        IKycRepository                KycRepo         { get; }
        ILoginHistoryRepository       LoginHistoryRepo { get; }
        IPaymentRepository            PaymentRepo     { get; }
        Task<int> SaveAsync();
    }
}
