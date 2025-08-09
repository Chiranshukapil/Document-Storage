namespace Document_Storage_System.Models;

using Document_Storage_System.Models.Entities;
using Microsoft.AspNetCore.Identity;

public class AppUser : IdentityUser
{
    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }
}
