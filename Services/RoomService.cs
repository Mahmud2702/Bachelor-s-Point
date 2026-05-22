using Bachelor_s_Point.Application.DTOs;
using Bachelor_s_Point.Application.Interfaces.Services;
using Bachelor_s_Point.Application.Interfaces.UnitOfWork;
using Bachelor_s_Point.Models;

namespace Bachelor_s_Point.Services
{
    public class RoomService : IRoomService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;
        private readonly ILogger<RoomService> _logger;

        public RoomService(IUnitOfWork unitOfWork, IEmailService emailService, ILogger<RoomService> logger)
        {
            _unitOfWork = unitOfWork;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<List<Room>> GetAllAvailableRoomsAsync()
            => await _unitOfWork.RoomRepo.GetAllAvailableWithOwnerAsync();

        public async Task<List<Room>> GetAllRoomsAsync()
            => await _unitOfWork.RoomRepo.GetAllWithOwnerAsync();

        public async Task<Room?> GetRoomByIdAsync(int id)
            => await _unitOfWork.RoomRepo.GetRoomWithOwnerByIdAsync(id);

        public async Task<List<Room>> GetMyRoomsAsync(int ownerUserId)
            => await _unitOfWork.RoomRepo.GetRoomsByOwnerIdAsync(ownerUserId);

        public async Task<List<Room>> SearchAsync(string searchText)
            => await _unitOfWork.RoomRepo.SearchAsync(searchText);

        public async Task<(string Result, int RoomId)> CreateRoomAsync(CreateRoomDto dto, int ownerUserId, bool autoApprove)
        {
            if (dto == null) return ("Invalid room data", 0);

            var owner = await _unitOfWork.UserRepo.GetByIdAsync(ownerUserId);
            if (owner == null) return ("Owner user not found", 0);

            var room = new Room
            {
                Title = dto.Title,
                Description = dto.Description,
                Price = dto.Price,
                WifiCost = dto.WifiCost,
                MealCostPerMonth = dto.MealCostPerMonth,
                MaidCostPerMonth = dto.MaidCostPerMonth,
                Location = dto.Location,
                UserId = ownerUserId,
                CreatedAt = DateTime.Now,
                IsAvailable = true,
                IsApproved = autoApprove,
                ApprovedAt = autoApprove ? DateTime.Now : null
            };

            await _unitOfWork.RoomRepo.AddAsync(room);
            await _unitOfWork.SaveAsync();
            return ("Success", room.Id);
        }

        public async Task<string> UpdateRoomAsync(Room room, int currentUserId, bool isAdmin)
        {
            var existing = await _unitOfWork.RoomRepo.GetByIdAsync(room.Id);
            if (existing == null) return "Room not found";
            if (!isAdmin && existing.UserId != currentUserId)
                return "You are not allowed to edit this room";

            existing.Title = room.Title;
            existing.Description = room.Description;
            existing.Price = room.Price;
            existing.Location = room.Location;
            existing.IsAvailable = room.IsAvailable;

            _unitOfWork.RoomRepo.Update(existing);
            await _unitOfWork.SaveAsync();
            return "Success";
        }

        public async Task<string> DeleteRoomAsync(int roomId, int currentUserId, bool isAdmin)
        {
            var existing = await _unitOfWork.RoomRepo.GetByIdAsync(roomId);
            if (existing == null) return "Room not found";
            if (!isAdmin && existing.UserId != currentUserId)
                return "You are not allowed to delete this room";

            _unitOfWork.RoomRepo.Delete(existing);
            await _unitOfWork.SaveAsync();
            return "Success";
        }

        public async Task<string> SelectRoomAsync(SelectRoomDto selection)
        {
            if (selection == null) return "Invalid selection";
            var room = await _unitOfWork.RoomRepo.GetRoomWithOwnerByIdAsync(selection.RoomId);
            if (room == null) return "Room not found";
            if (!room.IsApproved) return "This room has not been approved yet";
            if (!room.IsAvailable) return "This room is no longer available";

            var owner = room.Owner;
            if (owner == null || string.IsNullOrWhiteSpace(owner.Email))
                return "Room owner has no contactable email";

            var seeker = await _unitOfWork.UserRepo.GetByIdAsync(selection.SeekerUserId);
            if (seeker == null) return "Seeker user not found";
            if (seeker.Id == owner.Id) return "You cannot select your own room";

            var record = new RoomSelection
            {
                RoomId = room.Id,
                SeekerUserId = seeker.Id,
                Message = selection.Message,
                SelectedAt = DateTime.Now
            };
            await _unitOfWork.SelectionRepo.AddAsync(record);
            await _unitOfWork.SaveAsync();

            try
            {
                await _emailService.SendRoomSelectedNotificationAsync(room, owner, seeker, selection.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send room-selected email for RoomId={RoomId}", room.Id);
                return "Room selected, but the notification email could not be sent";
            }
            return "Success";
        }

        public async Task<List<RoomSelection>> GetMySelectionsAsync(int seekerUserId)
            => await _unitOfWork.SelectionRepo.GetBySeekerIdAsync(seekerUserId);

        public async Task<List<RoomSelection>> GetIncomingSelectionsAsync(int ownerUserId)
            => await _unitOfWork.SelectionRepo.GetByOwnerIdAsync(ownerUserId);

        public async Task<List<RoomSelection>> GetSelectionsForRoomAsync(int roomId)
            => await _unitOfWork.SelectionRepo.GetByRoomIdAsync(roomId);

        public async Task<List<Room>> GetPendingApprovalAsync()
            => await _unitOfWork.RoomRepo.GetPendingApprovalAsync();

        public async Task<string> ApproveRoomAsync(int roomId)
        {
            var room = await _unitOfWork.RoomRepo.GetByIdAsync(roomId);
            if (room == null) return "Room not found";
            if (room.IsApproved) return "Room is already approved";

            room.IsApproved = true;
            room.ApprovedAt = DateTime.Now;
            _unitOfWork.RoomRepo.Update(room);
            await _unitOfWork.SaveAsync();
            return "Success";
        }

        public async Task<PagedResult<Room>> GetApprovedPagedAsync(string? searchText, int page, int pageSize)
        {
            var (items, total) = await _unitOfWork.RoomRepo.GetApprovedAvailablePagedAsync(searchText, page, pageSize);
            return new PagedResult<Room>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = total
            };
        }

        public async Task AddRoomImageAsync(int roomId, string imagePath, bool isPrimary, int displayOrder)
        {
            var img = new RoomImage
            {
                RoomId = roomId,
                ImagePath = imagePath,
                IsPrimary = isPrimary,
                DisplayOrder = displayOrder,
                UploadedAt = DateTime.Now
            };
            await _unitOfWork.RoomImageRepo.AddAsync(img);
            await _unitOfWork.SaveAsync();
        }
    }
}
