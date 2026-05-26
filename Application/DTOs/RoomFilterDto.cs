namespace Bachelor_s_Point.Application.DTOs
{
    /// <summary>
    /// Carries every filter the Browse Rooms page supports.
    /// All properties are optional — a null/empty value means "don't filter on this".
    /// </summary>
    public class RoomFilterDto
    {
        public string? SearchText { get; set; }

        public string? Division { get; set; }
        public string? District { get; set; }

        public int? MinPrice { get; set; }
        public int? MaxPrice { get; set; }

        public bool HasWifi { get; set; }
        public bool HasMeal { get; set; }
        public bool HasMaid { get; set; }

        // Only show rooms currently available (approved rooms are always required)
        public bool AvailableOnly { get; set; }

        /// <summary>
        /// Sort key: "newest" (default), "price_asc", "price_desc", "oldest".
        /// </summary>
        public string? SortBy { get; set; }

        public int Page { get; set; } = 1;

        /// <summary>True if any actual filter (besides paging) is applied.</summary>
        public bool HasAnyFilter =>
            !string.IsNullOrWhiteSpace(SearchText) ||
            !string.IsNullOrWhiteSpace(Division) ||
            !string.IsNullOrWhiteSpace(District) ||
            MinPrice.HasValue || MaxPrice.HasValue ||
            HasWifi || HasMeal || HasMaid || AvailableOnly ||
            (!string.IsNullOrWhiteSpace(SortBy) && SortBy != "newest");
    }
}