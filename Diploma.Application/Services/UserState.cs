namespace Diploma.Application.Services
{
    public static class UserState
    {
        public static long UserId { get; set; }
        public static string FullName { get; set; } = "";  
        public static string Role { get; set; } = "";

        // ✅ Сравнение без учёта регистра — работает с "admin", "Admin", "ADMIN"
        public static bool IsAdmin => Role.Equals("admin", StringComparison.OrdinalIgnoreCase);
        public static bool IsOperator => Role.Equals("operator", StringComparison.OrdinalIgnoreCase);

        public static bool IsAuthenticated => !string.IsNullOrEmpty(FullName);

        public static void Set(long userId, string login, string role)
        {
            UserId = userId;
            FullName = login;
            Role = role;
        }

        public static void Clear()
        {
            UserId = 0;
            FullName = "";
            Role = "";
        }
    }
}