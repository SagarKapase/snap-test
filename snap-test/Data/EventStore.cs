using snap_test.Models;

namespace snap_test.Data
{
    /// <summary>
    /// In-memory seed store for events (15 records, Sept 2026 - Jan 2027). Some are sold out (ticketsSold == capacity) and some are free (price 0).
    /// Writes mutate this static list and reset on app restart (non-persistent, per PRD).
    /// </summary>
    public static class EventStore
    {
        public static List<Event> Events = new()
        {
            new Event { Id = 1, Name = "DevConf 2026", Category = "conference", Venue = "Moscone Center", City = "San Francisco", StartsAt = "2026-10-14T09:00:00Z", EndsAt = "2026-10-16T18:00:00Z", Price = 499.00m, Capacity = 5000, TicketsSold = 4312, Organizer = "DevConf Inc.", Tags = new() { "developers", "cloud", "ai" } },
            new Event { Id = 2, Name = "Jazz Under the Stars", Category = "concert", Venue = "Millennium Park", City = "Chicago", StartsAt = "2026-10-03T19:30:00Z", EndsAt = "2026-10-03T23:00:00Z", Price = 45.00m, Capacity = 2000, TicketsSold = 1780, Organizer = "Chicago Jazz Society", Tags = new() { "music", "jazz", "outdoor" } },
            new Event { Id = 3, Name = "City Marathon", Category = "sports", Venue = "Central Park", City = "New York", StartsAt = "2026-11-01T07:00:00Z", EndsAt = "2026-11-01T15:00:00Z", Price = 120.00m, Capacity = 30000, TicketsSold = 28750, Organizer = "NY Road Runners", Tags = new() { "running", "charity" } },
            new Event { Id = 4, Name = "Design Systems Summit", Category = "conference", Venue = "Barbican Centre", City = "London", StartsAt = "2026-11-12T09:00:00Z", EndsAt = "2026-11-13T17:30:00Z", Price = 349.00m, Capacity = 800, TicketsSold = 612, Organizer = "Pixelcraft Studios", Tags = new() { "design", "ux", "frontend" } },
            new Event { Id = 5, Name = "Rust Workshop", Category = "workshop", Venue = "Factory Berlin", City = "Berlin", StartsAt = "2026-10-20T10:00:00Z", EndsAt = "2026-10-20T16:00:00Z", Price = 89.00m, Capacity = 40, TicketsSold = 40, Organizer = "Rust Berlin Meetup", Tags = new() { "rust", "hands-on" } },
            new Event { Id = 6, Name = "Food Truck Festival", Category = "festival", Venue = "Waterfront Park", City = "Seattle", StartsAt = "2026-09-12T11:00:00Z", EndsAt = "2026-09-13T21:00:00Z", Price = 0.00m, Capacity = 10000, TicketsSold = 6420, Organizer = "Seattle Eats", Tags = new() { "food", "family", "free" } },
            new Event { Id = 7, Name = "AI Ethics Panel", Category = "meetup", Venue = "Kendall Square Hub", City = "Boston", StartsAt = "2026-10-08T18:00:00Z", EndsAt = "2026-10-08T20:00:00Z", Price = 0.00m, Capacity = 150, TicketsSold = 149, Organizer = "Aurora Health", Tags = new() { "ai", "ethics", "panel" } },
            new Event { Id = 8, Name = "Tokyo Game Expo", Category = "expo", Venue = "Makuhari Messe", City = "Tokyo", StartsAt = "2026-12-05T10:00:00Z", EndsAt = "2026-12-07T18:00:00Z", Price = 35.00m, Capacity = 60000, TicketsSold = 51200, Organizer = "Game Expo Committee", Tags = new() { "gaming", "expo" } },
            new Event { Id = 9, Name = "Startup Pitch Night", Category = "meetup", Venue = "WeWork Montreal", City = "Montreal", StartsAt = "2026-10-22T18:30:00Z", EndsAt = "2026-10-22T21:30:00Z", Price = 15.00m, Capacity = 120, TicketsSold = 87, Organizer = "Montreal Founders", Tags = new() { "startups", "networking" } },
            new Event { Id = 10, Name = "Symphony No. 9 Gala", Category = "concert", Venue = "Sydney Opera House", City = "Sydney", StartsAt = "2026-11-20T19:00:00Z", EndsAt = "2026-11-20T21:30:00Z", Price = 150.00m, Capacity = 2679, TicketsSold = 2500, Organizer = "Sydney Symphony", Tags = new() { "classical", "gala" } },
            new Event { Id = 11, Name = "Kubernetes Deep Dive", Category = "workshop", Venue = "Tech Park Bengaluru", City = "Bengaluru", StartsAt = "2026-10-10T09:30:00Z", EndsAt = "2026-10-10T17:30:00Z", Price = 59.00m, Capacity = 60, TicketsSold = 52, Organizer = "Nimbus Cloud Systems", Tags = new() { "kubernetes", "devops", "hands-on" } },
            new Event { Id = 12, Name = "Winter Film Festival", Category = "festival", Venue = "Cinémathèque Française", City = "Paris", StartsAt = "2027-01-15T10:00:00Z", EndsAt = "2027-01-22T23:00:00Z", Price = 80.00m, Capacity = 3000, TicketsSold = 1210, Organizer = "Paris Film Society", Tags = new() { "film", "international" } },
            new Event { Id = 13, Name = "Robotics Hackathon", Category = "hackathon", Venue = "Kitsune Robotics HQ", City = "Tokyo", StartsAt = "2026-11-07T09:00:00Z", EndsAt = "2026-11-08T17:00:00Z", Price = 0.00m, Capacity = 200, TicketsSold = 188, Organizer = "Kitsune Robotics", Tags = new() { "robotics", "hackathon", "prizes" } },
            new Event { Id = 14, Name = "Sustainable Energy Forum", Category = "conference", Venue = "ExCeL London", City = "London", StartsAt = "2026-09-24T08:30:00Z", EndsAt = "2026-09-25T17:00:00Z", Price = 275.00m, Capacity = 1500, TicketsSold = 1403, Organizer = "Solstice Energy", Tags = new() { "energy", "climate", "policy" } },
            new Event { Id = 15, Name = "Salsa Night", Category = "party", Venue = "La Bodeguita", City = "Miami", StartsAt = "2026-10-31T21:00:00Z", EndsAt = "2026-11-01T02:00:00Z", Price = 20.00m, Capacity = 300, TicketsSold = 95, Organizer = "Miami Dance Collective", Tags = new() { "dance", "nightlife" } }
        };
    }
}
