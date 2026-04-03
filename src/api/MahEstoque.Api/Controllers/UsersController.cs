using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MahEstoque.Api.DTOs;
using MahEstoque.Api.Services;
using MahEstoque.Api.Extensions;

namespace MahEstoque.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<ActionResult<List<UserListItemDto>>> GetAll()
    {
        if (!User.IsAdmin())
            return Forbid();

        var tenantId = User.GetTenantId();
        var users = await _userService.GetAllAsync(tenantId);
        return Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserListItemDto>> GetById(Guid id)
    {
        if (!User.IsAdmin())
            return Forbid();

        var tenantId = User.GetTenantId();
        var user = await _userService.GetByIdAsync(id, tenantId);
        if (user == null)
            return NotFound(new { message = "Usuário não encontrado" });
        return Ok(user);
    }

    [HttpPost]
    public async Task<ActionResult<UserListItemDto>> Create([FromBody] CreateUserRequest request)
    {
        if (!User.IsAdmin())
            return Forbid();

        var tenantId = User.GetTenantId();

        if (!await _userService.IsUsernameUniqueAsync(request.Username, tenantId))
            return BadRequest(new { message = "Nome de usuário já está em uso" });

        var user = await _userService.CreateAsync(request, tenantId);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<UserListItemDto>> Update(Guid id, [FromBody] UpdateUserRequest request)
    {
        if (!User.IsAdmin())
            return Forbid();

        var tenantId = User.GetTenantId();
        var user = await _userService.UpdateAsync(id, request, tenantId);
        return Ok(user);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        if (!User.IsAdmin())
            return Forbid();

        var tenantId = User.GetTenantId();

        var currentUserId = User.GetUserId();
        if (currentUserId == id)
            return BadRequest(new { message = "Você não pode excluir seu próprio usuário" });

        await _userService.DeleteAsync(id, tenantId);
        return NoContent();
    }
}