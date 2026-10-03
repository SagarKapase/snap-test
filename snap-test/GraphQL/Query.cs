using snap_test.Data;
using snap_test.Models;

namespace snap_test.GraphQL
{
    public class Query
    {
        public IEnumerable<User> GetUsers() => UserData.Users;

        public User? GetUser(int id) =>
            UserData.Users.FirstOrDefault(u => u.Id == id);

        // -------------------- PRODUCTS / POSTS / TODOS (TestingAPIs stores) --------------------
        public List<Product> GetProducts(string? category) =>
            Snapshot(ProductStore.Products, p => category == null || Eq(p.Category, category));

        public Product? GetProduct(int id) => Find(ProductStore.Products, p => p.Id == id);

        public List<Post> GetPosts(int? userId) =>
            Snapshot(PostStore.Posts, p => userId == null || p.UserId == userId);

        public Post? GetPost(int id) => Find(PostStore.Posts, p => p.Id == id);

        public List<Todo> GetTodos(bool? completed) =>
            Snapshot(TodoStore.Todos, t => completed == null || t.Completed == completed);

        // -------------------- EMPLOYEES / COMPANIES --------------------
        public List<Employee> GetEmployees(string? department) =>
            Snapshot(EmployeeStore.Employees, e => department == null || Eq(e.Department, department));

        public Employee? GetEmployee(int id) => Find(EmployeeStore.Employees, e => e.Id == id);

        public List<Company> GetCompanies() => Snapshot(CompanyStore.Companies, _ => true);

        // -------------------- BOOKS / MOVIES --------------------
        public List<Book> GetBooks(string? genre) =>
            Snapshot(BookStore.Books, b => genre == null || Eq(b.Genre, genre));

        public Book? GetBook(int id) => Find(BookStore.Books, b => b.Id == id);

        public List<Movie> GetMovies(string? genre) =>
            Snapshot(MovieStore.Movies, m => genre == null || m.Genres.Any(g => Eq(g, genre)));

        public Movie? GetMovie(int id) => Find(MovieStore.Movies, m => m.Id == id);

        // -------------------- COUNTRIES --------------------
        public List<Country> GetCountries(string? region) =>
            Snapshot(CountryStore.Countries, c => region == null || Eq(c.Region, region));

        public Country? GetCountry(string code) =>
            Find(CountryStore.Countries, c => Eq(c.Code, code) || Eq(c.Code3, code));

        // -------------------- PEOPLE / EVENTS / ALBUMS / PHOTOS --------------------
        public List<Person> GetPeople() => Snapshot(PersonStore.People, _ => true);

        public List<Event> GetEvents(string? category) =>
            Snapshot(EventStore.Events, e => category == null || Eq(e.Category, category));

        public List<Album> GetAlbums(int? userId) =>
            Snapshot(AlbumStore.Albums, a => userId == null || a.UserId == userId);

        public List<Photo> GetPhotos(int? albumId) =>
            Snapshot(PhotoStore.Photos, p => albumId == null || p.AlbumId == albumId);

        // Copy under the store lock so a concurrent REST write can't break enumeration.
        private static List<T> Snapshot<T>(List<T> store, Func<T, bool> predicate)
        {
            lock (store) return store.Where(predicate).ToList();
        }

        private static T? Find<T>(List<T> store, Func<T, bool> predicate) where T : class
        {
            lock (store) return store.FirstOrDefault(predicate);
        }

        private static bool Eq(string? a, string? b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
    }

}
