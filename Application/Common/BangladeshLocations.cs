namespace Bachelor_s_Point.Application.Common
{
    /// <summary>
    /// Fixed list of Bangladesh divisions and their districts.
    /// Used to populate the cascading Division -> District dropdowns
    /// in the room post form and the browse-rooms filter.
    /// </summary>
    public static class BangladeshLocations
    {
        public static readonly IReadOnlyDictionary<string, string[]> DivisionDistricts =
            new Dictionary<string, string[]>
            {
                ["Dhaka"] = new[]
                {
                    "Dhaka", "Gazipur", "Narayanganj", "Tangail", "Kishoreganj",
                    "Manikganj", "Munshiganj", "Narsingdi", "Faridpur", "Gopalganj",
                    "Madaripur", "Rajbari", "Shariatpur"
                },
                ["Chittagong"] = new[]
                {
                    "Chittagong", "Cox's Bazar", "Cumilla", "Brahmanbaria", "Chandpur",
                    "Feni", "Khagrachhari", "Bandarban", "Rangamati", "Lakshmipur",
                    "Noakhali"
                },
                ["Rajshahi"] = new[]
                {
                    "Rajshahi", "Bogura", "Joypurhat", "Naogaon", "Natore",
                    "Chapainawabganj", "Pabna", "Sirajganj"
                },
                ["Khulna"] = new[]
                {
                    "Khulna", "Bagerhat", "Chuadanga", "Jashore", "Jhenaidah",
                    "Kushtia", "Magura", "Meherpur", "Narail", "Satkhira"
                },
                ["Barishal"] = new[]
                {
                    "Barishal", "Barguna", "Bhola", "Jhalokathi", "Patuakhali", "Pirojpur"
                },
                ["Sylhet"] = new[]
                {
                    "Sylhet", "Habiganj", "Moulvibazar", "Sunamganj"
                },
                ["Rangpur"] = new[]
                {
                    "Rangpur", "Dinajpur", "Gaibandha", "Kurigram", "Lalmonirhat",
                    "Nilphamari", "Panchagarh", "Thakurgaon"
                },
                ["Mymensingh"] = new[]
                {
                    "Mymensingh", "Jamalpur", "Netrokona", "Sherpur"
                }
            };

        public static IEnumerable<string> Divisions => DivisionDistricts.Keys;

        public static string[] GetDistricts(string? division)
        {
            if (!string.IsNullOrWhiteSpace(division) &&
                DivisionDistricts.TryGetValue(division, out var districts))
                return districts;
            return Array.Empty<string>();
        }
    }
}