using HotChocolate;
using snap_test.Data;
using snap_test.Models;

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

        public User? UpdateUser(int id, string name, string job, string city)
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

        // -------------------- TODOS (shared with /api/todos) --------------------
        public Todo AddTodo(string title, int userId, string priority = "medium")
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new GraphQLException("Field 'title' is required.");
            if (!new[] { "high", "medium", "low" }.Contains(priority.ToLowerInvariant()))
                throw new GraphQLException("Field 'priority' must be one of: high, medium, low.");

            lock (TodoStore.Todos)
            {
                var todo = new Todo
                {
                    Id = TodoStore.Todos.Any() ? TodoStore.Todos.Max(t => t.Id) + 1 : 1,
                    UserId = userId,
                    Title = title,
                    Completed = false,
                    Priority = priority.ToLowerInvariant(),
                    DueDate = DateTime.UtcNow.AddDays(7).ToString("yyyy-MM-dd")
                };

                TodoStore.Todos.Add(todo);
                return todo;
            }
        }

        public Todo? ToggleTodo(int id)
        {
            lock (TodoStore.Todos)
            {
                var todo = TodoStore.Todos.FirstOrDefault(t => t.Id == id);
                if (todo == null) return null;

                todo.Completed = !todo.Completed;
                return todo;
            }
        }

        public bool DeleteTodo(int id)
        {
            lock (TodoStore.Todos)
            {
                return TodoStore.Todos.RemoveAll(t => t.Id == id) > 0;
            }
        }
    }

}
