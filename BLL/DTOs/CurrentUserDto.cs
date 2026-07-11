using DAL.Entities;

namespace BLL.DTOs;

public class CurrentUserDto
{
    public int UserId { get; set; }

    public UserRole Role { get; set; }
}
