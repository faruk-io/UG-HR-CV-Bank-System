using CvBank.Api.Auth;
using CvBank.Api.Models;
using CvBank.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CvBank.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController(UserService users) : ControllerBase
{
    private SignedInUser Me => SignedInUser.From(User);

    [HttpGet]
    [Permission(Permissions.UserManage)]
    public async Task<ActionResult<PagedResult<UserListItemDto>>> List([FromQuery] UserQuery query) =>
        Ok(await users.ListAsync(query));

    [HttpGet("roles")]
    [Permission(Permissions.UserManage)]
    public async Task<ActionResult<IReadOnlyList<RoleDto>>> Roles() => Ok(await users.RolesAsync());

    [HttpPost]
    [Permission(Permissions.UserManage)]
    public async Task<ActionResult<UserListItemDto>> Create(UserCreateRequest request) =>
        Ok(await users.CreateAsync(request, Me));

    [HttpPut("{id:int}")]
    [Permission(Permissions.UserManage)]
    public async Task<ActionResult<UserListItemDto>> Update(int id, UserUpdateRequest request) =>
        Ok(await users.UpdateAsync(id, request, Me));

    [HttpDelete("{id:int}")]
    [Permission(Permissions.UserManage)]
    public async Task<IActionResult> Remove(int id)
    {
        await users.RemoveAsync(id, Me);
        return NoContent();
    }
}
