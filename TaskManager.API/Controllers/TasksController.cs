using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Application.Features.Tasks.Commands;
using TaskManager.Application.Features.Tasks.Queries;

namespace TaskManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]

// ── AUTHORIZE ─────────────────────────────────────────
// Every endpoint in this controller requires a valid JWT token
// Without token → 401 Unauthorized automatically
// No need to check auth in each method individually
[Authorize]
// ─────────────────────────────────────────────────────
public class TasksController : ControllerBase
{
    private readonly IMediator _mediator;

    public TasksController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _mediator.Send(new GetAllTasksQuery());
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.ErrorMessage);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _mediator.Send(new GetTaskByIdQuery(id));
        return result.IsSuccess ? Ok(result.Data) : NotFound(result.ErrorMessage);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTaskCommand command)
    {
        var result = await _mediator.Send(command);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.ErrorMessage);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTaskCommand command)
    {
        if (id != command.Id)
            return BadRequest("Id mismatch");

        var result = await _mediator.Send(command);
        return result.IsSuccess ? Ok(result.Data) : NotFound(result.ErrorMessage);
    }

    // ── ROLE BASED ACCESS ─────────────────────────────────
// [Authorize] on class = any logged in user
// [Authorize(Roles = "Admin")] on method = Admin only
// Regular users get 403 Forbidden if they try to delete
[HttpDelete("{id}")]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> Delete(int id)
{
    var result = await _mediator.Send(new DeleteTaskCommand(id));
    return result.IsSuccess ? Ok("Task deleted successfully") : NotFound(result.ErrorMessage);
}
// ─────────────────────────────────────────────────────
}