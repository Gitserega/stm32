namespace Diploma.Entity;

public class User
{
    

    public long Id { get; set; }
    public string Login { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public long RoleId { get; set; }
    public Role Role { get; set; }
}