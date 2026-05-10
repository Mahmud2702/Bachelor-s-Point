namespace Bachelor_s_Point.Application.DTOs
{
    public class RoomDto
    {
        public int Id { get; set; }

        public string? Title { get; set; }

        public string? Description { get; set; }

        public int Price { get; set; }

        public string? Location { get; set; }

        public int UserId { get; set; }

        public string? OwnerName { get; set; }

        public string? OwnerEmail { get; set; }

        public DateTime CreatedAt { get; set; }

        public bool IsAvailable { get; set; }
    }
}
