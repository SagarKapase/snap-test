using snap_test.Models;

namespace snap_test.Data
{
    /// <summary>
    /// In-memory seed store for quotes (50 records) across 5 categories:
    /// programming, motivation, wisdom, humor, design.
    /// </summary>
    public static class QuoteStore
    {
        public static List<Quote> Quotes = new()
        {
            // ---- programming (10) ----
            new Quote { Id = 1, Text = "First, solve the problem. Then, write the code.", Author = "John Johnson", Category = "programming" },
            new Quote { Id = 2, Text = "Code is like humor. When you have to explain it, it's bad.", Author = "Cory House", Category = "programming" },
            new Quote { Id = 3, Text = "Programs must be written for people to read, and only incidentally for machines to execute.", Author = "Harold Abelson", Category = "programming" },
            new Quote { Id = 4, Text = "Any fool can write code that a computer can understand. Good programmers write code that humans can understand.", Author = "Martin Fowler", Category = "programming" },
            new Quote { Id = 5, Text = "Simplicity is the soul of efficiency.", Author = "Austin Freeman", Category = "programming" },
            new Quote { Id = 6, Text = "Make it work, make it right, make it fast.", Author = "Kent Beck", Category = "programming" },
            new Quote { Id = 7, Text = "The best error message is the one that never shows up.", Author = "Thomas Fuchs", Category = "programming" },
            new Quote { Id = 8, Text = "Talk is cheap. Show me the code.", Author = "Linus Torvalds", Category = "programming" },
            new Quote { Id = 9, Text = "Premature optimization is the root of all evil.", Author = "Donald Knuth", Category = "programming" },
            new Quote { Id = 10, Text = "Deleted code is debugged code.", Author = "Jeff Sickel", Category = "programming" },

            // ---- motivation (10) ----
            new Quote { Id = 11, Text = "The only way to do great work is to love what you do.", Author = "Steve Jobs", Category = "motivation" },
            new Quote { Id = 12, Text = "Success is not final, failure is not fatal: it is the courage to continue that counts.", Author = "Winston Churchill", Category = "motivation" },
            new Quote { Id = 13, Text = "Believe you can and you're halfway there.", Author = "Theodore Roosevelt", Category = "motivation" },
            new Quote { Id = 14, Text = "It always seems impossible until it's done.", Author = "Nelson Mandela", Category = "motivation" },
            new Quote { Id = 15, Text = "Don't watch the clock; do what it does. Keep going.", Author = "Sam Levenson", Category = "motivation" },
            new Quote { Id = 16, Text = "The future belongs to those who believe in the beauty of their dreams.", Author = "Eleanor Roosevelt", Category = "motivation" },
            new Quote { Id = 17, Text = "Hardships often prepare ordinary people for an extraordinary destiny.", Author = "C.S. Lewis", Category = "motivation" },
            new Quote { Id = 18, Text = "Your limitation—it's only your imagination.", Author = "Anonymous", Category = "motivation" },
            new Quote { Id = 19, Text = "Great things never come from comfort zones.", Author = "Anonymous", Category = "motivation" },
            new Quote { Id = 20, Text = "Push yourself, because no one else is going to do it for you.", Author = "Anonymous", Category = "motivation" },

            // ---- wisdom (10) ----
            new Quote { Id = 21, Text = "The only true wisdom is in knowing you know nothing.", Author = "Socrates", Category = "wisdom" },
            new Quote { Id = 22, Text = "In the middle of difficulty lies opportunity.", Author = "Albert Einstein", Category = "wisdom" },
            new Quote { Id = 23, Text = "Life is really simple, but we insist on making it complicated.", Author = "Confucius", Category = "wisdom" },
            new Quote { Id = 24, Text = "The journey of a thousand miles begins with one step.", Author = "Lao Tzu", Category = "wisdom" },
            new Quote { Id = 25, Text = "Knowing yourself is the beginning of all wisdom.", Author = "Aristotle", Category = "wisdom" },
            new Quote { Id = 26, Text = "We are what we repeatedly do. Excellence, then, is not an act, but a habit.", Author = "Will Durant", Category = "wisdom" },
            new Quote { Id = 27, Text = "The unexamined life is not worth living.", Author = "Socrates", Category = "wisdom" },
            new Quote { Id = 28, Text = "Patience is bitter, but its fruit is sweet.", Author = "Aristotle", Category = "wisdom" },
            new Quote { Id = 29, Text = "He who knows others is wise; he who knows himself is enlightened.", Author = "Lao Tzu", Category = "wisdom" },
            new Quote { Id = 30, Text = "Turn your wounds into wisdom.", Author = "Oprah Winfrey", Category = "wisdom" },

            // ---- humor (10) ----
            new Quote { Id = 31, Text = "I'm not lazy, I'm on energy-saving mode.", Author = "Anonymous", Category = "humor" },
            new Quote { Id = 32, Text = "There are 10 types of people in the world: those who understand binary and those who don't.", Author = "Anonymous", Category = "humor" },
            new Quote { Id = 33, Text = "I would love to change the world, but they won't give me the source code.", Author = "Anonymous", Category = "humor" },
            new Quote { Id = 34, Text = "A programmer is a person who fixes a problem you didn't know you had, in a way you don't understand.", Author = "Anonymous", Category = "humor" },
            new Quote { Id = 35, Text = "To err is human, but to really foul things up you need a computer.", Author = "Paul Ehrlich", Category = "humor" },
            new Quote { Id = 36, Text = "My code doesn't work, I have no idea why. My code works, I have no idea why.", Author = "Anonymous", Category = "humor" },
            new Quote { Id = 37, Text = "Weeks of coding can save you hours of planning.", Author = "Anonymous", Category = "humor" },
            new Quote { Id = 38, Text = "It's not a bug, it's an undocumented feature.", Author = "Anonymous", Category = "humor" },
            new Quote { Id = 39, Text = "Real programmers count from 0.", Author = "Anonymous", Category = "humor" },
            new Quote { Id = 40, Text = "There is no place like 127.0.0.1.", Author = "Anonymous", Category = "humor" },

            // ---- design (10) ----
            new Quote { Id = 41, Text = "Design is not just what it looks like and feels like. Design is how it works.", Author = "Steve Jobs", Category = "design" },
            new Quote { Id = 42, Text = "Good design is as little design as possible.", Author = "Dieter Rams", Category = "design" },
            new Quote { Id = 43, Text = "Simplicity is the ultimate sophistication.", Author = "Leonardo da Vinci", Category = "design" },
            new Quote { Id = 44, Text = "Design is intelligence made visible.", Author = "Alina Wheeler", Category = "design" },
            new Quote { Id = 45, Text = "People ignore design that ignores people.", Author = "Frank Chimero", Category = "design" },
            new Quote { Id = 46, Text = "Everything is designed. Few things are designed well.", Author = "Brian Reed", Category = "design" },
            new Quote { Id = 47, Text = "The details are not the details. They make the design.", Author = "Charles Eames", Category = "design" },
            new Quote { Id = 48, Text = "Content precedes design. Design in the absence of content is not design, it's decoration.", Author = "Jeffrey Zeldman", Category = "design" },
            new Quote { Id = 49, Text = "Good design is obvious. Great design is transparent.", Author = "Joe Sparano", Category = "design" },
            new Quote { Id = 50, Text = "Design adds value faster than it adds costs.", Author = "Joel Spolsky", Category = "design" }
        };
    }
}
