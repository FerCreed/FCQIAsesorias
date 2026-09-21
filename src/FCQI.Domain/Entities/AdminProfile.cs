namespace FCQI.Domain.Entities;

public class AdminProfile
{
    public int PersonId { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    public Person Person { get; set; } = null!;
}
