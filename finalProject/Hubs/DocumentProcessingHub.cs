using System.Security.Claims;
using finalProject.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace finalProject.Hubs;

[Authorize]
public class DocumentProcessingHub : Hub
{
    public const string AdminGroup = "documents-admins";

    public static string GetTeacherGroupName(int teacherId)
    {
        return $"documents-teacher-{teacherId}";
    }

    public override async Task OnConnectedAsync()
    {
        if (Context.User?.IsInRole(StudyMateRoles.Admin) == true)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, AdminGroup);
        }

        if (Context.User?.IsInRole(StudyMateRoles.Teacher) == true)
        {
            var userIdValue = Context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdValue, out var teacherId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, GetTeacherGroupName(teacherId));
            }
        }

        await base.OnConnectedAsync();
    }
}
