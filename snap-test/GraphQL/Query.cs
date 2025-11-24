namespace snap_test.GraphQL
{
    public class Query
    {
        public IEnumerable<User> GetUsers() => UserData.Users;

        public User GetUser(int id) =>
            UserData.Users.FirstOrDefault(u => u.Id == id);
    }

}
