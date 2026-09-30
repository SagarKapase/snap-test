using snap_test.Models;

namespace snap_test.Data
{
    /// <summary>
    /// In-memory seed store for books (25 records across 7 genres). ISBNs are real ISBN-13s.
    /// Writes mutate this static list and reset on app restart (non-persistent, per PRD).
    /// </summary>
    public static class BookStore
    {
        public static List<Book> Books = new()
        {
            new Book { Id = 1, Title = "Clean Code", Author = "Robert C. Martin", Isbn = "9780132350884", Genre = "programming", PublishedYear = 2008, Pages = 464, Language = "English", Price = 37.99m, Rating = 4.4, Available = true, Tags = new() { "craftsmanship", "refactoring" } },
            new Book { Id = 2, Title = "The Pragmatic Programmer", Author = "Andrew Hunt & David Thomas", Isbn = "9780135957059", Genre = "programming", PublishedYear = 2019, Pages = 352, Language = "English", Price = 44.99m, Rating = 4.7, Available = true, Tags = new() { "career", "best-practices" } },
            new Book { Id = 3, Title = "Designing Data-Intensive Applications", Author = "Martin Kleppmann", Isbn = "9781449373320", Genre = "programming", PublishedYear = 2017, Pages = 616, Language = "English", Price = 49.99m, Rating = 4.8, Available = true, Tags = new() { "databases", "distributed-systems" } },
            new Book { Id = 4, Title = "Refactoring", Author = "Martin Fowler", Isbn = "9780134757599", Genre = "programming", PublishedYear = 2018, Pages = 448, Language = "English", Price = 47.99m, Rating = 4.5, Available = false, Tags = new() { "refactoring", "design" } },
            new Book { Id = 5, Title = "Code Complete", Author = "Steve McConnell", Isbn = "9780735619678", Genre = "programming", PublishedYear = 2004, Pages = 960, Language = "English", Price = 39.99m, Rating = 4.6, Available = true, Tags = new() { "construction", "classic" } },
            new Book { Id = 6, Title = "Dune", Author = "Frank Herbert", Isbn = "9780441172719", Genre = "science-fiction", PublishedYear = 1965, Pages = 688, Language = "English", Price = 10.99m, Rating = 4.6, Available = true, Tags = new() { "classic", "space-opera" } },
            new Book { Id = 7, Title = "The Left Hand of Darkness", Author = "Ursula K. Le Guin", Isbn = "9780441478125", Genre = "science-fiction", PublishedYear = 1969, Pages = 304, Language = "English", Price = 9.99m, Rating = 4.3, Available = true, Tags = new() { "classic", "gender" } },
            new Book { Id = 8, Title = "Neuromancer", Author = "William Gibson", Isbn = "9780441569595", Genre = "science-fiction", PublishedYear = 1984, Pages = 271, Language = "English", Price = 9.99m, Rating = 4.1, Available = false, Tags = new() { "cyberpunk" } },
            new Book { Id = 9, Title = "The Three-Body Problem", Author = "Liu Cixin", Isbn = "9780765382030", Genre = "science-fiction", PublishedYear = 2008, Pages = 400, Language = "Chinese", Price = 11.99m, Rating = 4.2, Available = true, Tags = new() { "hard-sf", "translated" } },
            new Book { Id = 10, Title = "Foundation", Author = "Isaac Asimov", Isbn = "9780553293357", Genre = "science-fiction", PublishedYear = 1951, Pages = 255, Language = "English", Price = 8.99m, Rating = 4.3, Available = true, Tags = new() { "classic", "galactic-empire" } },
            new Book { Id = 11, Title = "Pride and Prejudice", Author = "Jane Austen", Isbn = "9780141439518", Genre = "classics", PublishedYear = 1813, Pages = 480, Language = "English", Price = 7.99m, Rating = 4.3, Available = true, Tags = new() { "romance", "regency" } },
            new Book { Id = 12, Title = "One Hundred Years of Solitude", Author = "Gabriel García Márquez", Isbn = "9780060883287", Genre = "classics", PublishedYear = 1967, Pages = 417, Language = "Spanish", Price = 14.99m, Rating = 4.1, Available = true, Tags = new() { "magical-realism", "translated" } },
            new Book { Id = 13, Title = "Crime and Punishment", Author = "Fyodor Dostoevsky", Isbn = "9780143058144", Genre = "classics", PublishedYear = 1866, Pages = 671, Language = "Russian", Price = 12.99m, Rating = 4.3, Available = false, Tags = new() { "psychological", "translated" } },
            new Book { Id = 14, Title = "The Great Gatsby", Author = "F. Scott Fitzgerald", Isbn = "9780743273565", Genre = "classics", PublishedYear = 1925, Pages = 180, Language = "English", Price = 10.99m, Rating = 3.9, Available = true, Tags = new() { "jazz-age" } },
            new Book { Id = 15, Title = "Sapiens", Author = "Yuval Noah Harari", Isbn = "9780062316097", Genre = "history", PublishedYear = 2011, Pages = 464, Language = "Hebrew", Price = 18.99m, Rating = 4.4, Available = true, Tags = new() { "anthropology", "bestseller" } },
            new Book { Id = 16, Title = "Guns, Germs, and Steel", Author = "Jared Diamond", Isbn = "9780393354324", Genre = "history", PublishedYear = 1997, Pages = 528, Language = "English", Price = 17.99m, Rating = 4.0, Available = true, Tags = new() { "civilization" } },
            new Book { Id = 17, Title = "The Silk Roads", Author = "Peter Frankopan", Isbn = "9781101912379", Genre = "history", PublishedYear = 2015, Pages = 672, Language = "English", Price = 19.99m, Rating = 4.2, Available = false, Tags = new() { "trade", "asia" } },
            new Book { Id = 18, Title = "Atomic Habits", Author = "James Clear", Isbn = "9780735211292", Genre = "self-help", PublishedYear = 2018, Pages = 320, Language = "English", Price = 16.99m, Rating = 4.4, Available = true, Tags = new() { "habits", "productivity" } },
            new Book { Id = 19, Title = "Deep Work", Author = "Cal Newport", Isbn = "9781455586691", Genre = "self-help", PublishedYear = 2016, Pages = 304, Language = "English", Price = 15.99m, Rating = 4.2, Available = true, Tags = new() { "focus", "productivity" } },
            new Book { Id = 20, Title = "Thinking, Fast and Slow", Author = "Daniel Kahneman", Isbn = "9780374533557", Genre = "psychology", PublishedYear = 2011, Pages = 512, Language = "English", Price = 17.00m, Rating = 4.2, Available = true, Tags = new() { "behavioral-economics" } },
            new Book { Id = 21, Title = "Man's Search for Meaning", Author = "Viktor E. Frankl", Isbn = "9780807014295", Genre = "psychology", PublishedYear = 1946, Pages = 184, Language = "German", Price = 9.99m, Rating = 4.7, Available = true, Tags = new() { "philosophy", "memoir" } },
            new Book { Id = 22, Title = "The Hobbit", Author = "J.R.R. Tolkien", Isbn = "9780547928227", Genre = "fantasy", PublishedYear = 1937, Pages = 300, Language = "English", Price = 12.99m, Rating = 4.6, Available = true, Tags = new() { "classic", "adventure" } },
            new Book { Id = 23, Title = "A Wizard of Earthsea", Author = "Ursula K. Le Guin", Isbn = "9780547773742", Genre = "fantasy", PublishedYear = 1968, Pages = 183, Language = "English", Price = 9.99m, Rating = 4.0, Available = true, Tags = new() { "magic", "coming-of-age" } },
            new Book { Id = 24, Title = "The Name of the Wind", Author = "Patrick Rothfuss", Isbn = "9780756404741", Genre = "fantasy", PublishedYear = 2007, Pages = 662, Language = "English", Price = 11.99m, Rating = 4.5, Available = false, Tags = new() { "epic", "magic" } },
            new Book { Id = 25, Title = "Mistborn: The Final Empire", Author = "Brandon Sanderson", Isbn = "9780765350381", Genre = "fantasy", PublishedYear = 2006, Pages = 541, Language = "English", Price = 10.99m, Rating = 4.5, Available = true, Tags = new() { "heist", "magic-system" } }
        };
    }
}
