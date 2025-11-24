namespace snap_test.GraphQL
{
    public class Mutation
    {
        public User CreateUser(string name, string job, string city)
        {
            var newUser = new User
            {
                Id = new Random().Next(1000, 9999),
                Name = name,
                Job = job,
                City = city
            };

            UserData.Users.Add(newUser);
            return newUser;
        }

        public User UpdateUser(int id, string name, string job, string city)
        {
            var user = UserData.Users.FirstOrDefault(u => u.Id == id);
            if (user == null) return null;

            user.Name = name;
            user.Job = job;
            user.City = city;
            return user;
        }

        public string DeleteUser(int id)
        {
            var user = UserData.Users.FirstOrDefault(u => u.Id == id);
            if (user == null)
                return $"User {id} not found";

            UserData.Users.Remove(user);
            return $"User {id} deleted successfully";
        }
    }

}
