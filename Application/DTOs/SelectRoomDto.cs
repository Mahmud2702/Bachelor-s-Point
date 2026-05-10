namespace Bachelor_s_Point.Application.DTOs
{
    /// <summary>
    /// Carries data captured when a Room Seeker selects a room.
    /// Used by RoomService and EmailService to build the notification.
    /// </summary>
    public class SelectRoomDto
    {
        public int RoomId { get; set; }

        public int SeekerUserId { get; set; }

        public string? Message { get; set; }
    }
}
