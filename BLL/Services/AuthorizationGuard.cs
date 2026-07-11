using BLL.DTOs;
using DAL.Entities;

namespace BLL.Services;

internal static class AuthorizationGuard
{
    public static void RequireRole(CurrentUserDto currentUser, UserRole role)
    {
        if (currentUser.Role != role)
        {
            throw new UnauthorizedAccessException($"This operation requires the {role} role.");
        }
    }

    public static void RequireDocumentOwner(CurrentUserDto currentUser, Document document)
    {
        RequireRole(currentUser, UserRole.Teacher);

        if (document.UploadedByTeacherId != currentUser.UserId)
        {
            throw new UnauthorizedAccessException("Teachers can only manage documents they uploaded.");
        }
    }
}
