namespace QuickBooksDemo.Models.Entities;

public class Technician
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<string> Specialties { get; set; } = new List<string>();
    public bool Active { get; set; } = true;

    // Navigation property
    public virtual ICollection<Job> Jobs { get; set; } = new List<Job>();
}