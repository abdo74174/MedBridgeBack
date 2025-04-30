public class UpdateUserDto
{
    public string Email { get; set; }
    public string Name { get; set; }
    public string MedicalSpecialist { get; set; }
    public string? Phone { get; set; }  // Make Phone optional
    public string? Address { get; set; }
}
