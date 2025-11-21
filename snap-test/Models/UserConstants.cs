namespace snap_test.Models
{
    public class UserConstants
    {
        public static List<UserAuth> Users = new()
            {
                    new UserAuth(){ Username="Michael",Password="Thompson",Role="Admin"}
            };
    }
}
