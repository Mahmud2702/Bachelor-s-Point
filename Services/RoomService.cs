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

        public RoomService(
            IUnitOfWork unitOfWork,
            IEmailService emailService,
            ILogger<RoomService> logger)
        {
            _unitOfWork = unitOfWork;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<List<Room>> GetAllAvailableRoomsAsync()
        {
            return await _unitOfWork.RoomRepo.GetAllAvailableWithOwnerAsync();
        }

        public async Task<List<Room>> GetAllRoomsAsync()
        {
            return await _unitOfWork.RoomRepo.GetAllWithOwnerAsync();
        }

        public async Task<Room?> GetRoomByIdAsync(int id)
        {
            return await _unitOfWork.RoomRepo.GetRoomWithOwnerByIdAsync(id);
        }

        public async Task<List<Room>> GetMyRoomsAsync(int ownerUserId)
        {
            return await _unitOfWork.RoomRepo.GetRoomsByOwnerIdAsync(ownerUserId);
        }

        public async Task<List<Room>> SearchAsync(string searchText)
        {
            return await _unitOfWork.RoomRepo.SearchAsync(searchText);
        }

        public async Task<string> CreateRoomAsync(CreateRoomDto dto, int ownerUserId)
        {
            if (dto == null)
            {
                return "Invalid room data";
            }

            var owner = await _unitOfWork.UserRepo.GetByIdAsync(ownerUserId);

            if (owner == null)
            {
                return "Owner user not found";
            }

            var room = new Room
            {
                Title = dto.Title,
                Description = dto.Description,
                Price = dto.Price,
                Location = dto.Location,
                UserId = ownerUserId,
                CreatedAt = DateTime.Now,
                IsAvailable = true
            };

            await _unitOfWork.RoomRepo.AddAsync(room);
            await _unitOfWork.SaveAsync();

            return "Success";
        }

        public async Task<string> UpdateRoomAsync(Room room, int currentUserId, bool isAdmin)
        {
            var existing = await _unitOfWork.RoomRepo.GetByIdAsync(room.Id);

            if (existing == null)
            {
                return "Room not found";
            }

            // Authorization: only the owner of the room or an admin can edit it
            if (!isAdmin && existing.UserId != currentUserId)
            {
                return "You are not allowed to edit this room";
            }

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

            if (existing == null)
            {
                return "Room not found";
            }

            if (!isAdmin && existing.UserId != currentUserId)
            {
                return "You are not allowed to delete this room";
            }

            _unitOfWork.RoomRepo.Delete(existing);
            await _unitOfWork.SaveAsync();

            return "Success";
        }

        // ---------------------------------------------------------------
        // The core "Select Room" workflow per the project documentation.
        //   1. Repository fetches the room (with owner).
        //   2. Owner is identified using room.UserId.
        //   3. UnitOfWork commits anything that changed.
        //   4. EmailService is triggered to notify the owner.
        // ---------------------------------------------------------------
        public async Task<string> SelectRoomAsync(SelectRoomDto selection)
        {
            if (selection == null)
            {
                return "Invalid selection";
            }

            // Step 1: Repository Layer — fetch the room with its owner
            var room = await _unitOfWork.RoomRepo.GetRoomWithOwnerByIdAsync(selection.RoomId);

            if (room == null)
            {
                return "Room not found";
            }

            if (!room.IsAvailable)
            {
                return "This room is no longer available";
            }

            // Step 2: identify the owner from room.UserId
            var owner = room.Owner;

            if (owner == null || string.IsNullOrWhiteSpace(owner.Email))
            {
                return "Room owner has no contactable email";
            }

            // Step 3: get the seeker
            var seeker = await _unitOfWork.UserRepo.GetByIdAsync(selection.SeekerUserId);

            if (seeker == null)
            {
                return "Seeker user not found";
            }

            if (seeker.Id == owner.Id)
            {
                return "You cannot select your own room";
            }

            // Step 4: UnitOfWork commit — even if we don't change DB rows here,
            // the pattern keeps every workflow under a single transaction boundary.
            await _unitOfWork.SaveAsync();

            // Step 5: Email Service Trigger (don't fail the operation if email fails)
            try
            {
                await _emailService.SendRoomSelectedNotificationAsync(
                    room, owner, seeker, selection.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send room-selected email for RoomId={RoomId}", room.Id);
                return "Room selected, but the notification email could not be sent";
            }

            return "Success";
        }
    }
}
