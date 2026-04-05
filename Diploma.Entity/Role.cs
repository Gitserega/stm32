namespace Diploma.Entity;

public class Role
{
    public long Id { get; set; }
    public string Name { get; set; } =  string.Empty;
    public List<User> Users { get; set; }
}