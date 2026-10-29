using CourseWaitlist.Api.Models;
using CourseWaitlist.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace CourseWaitlist.Api.Controllers;

[ApiController, Route("api/course"), Authorize]
public sealed class EnrollmentsController(IEnrollmentService service) : ControllerBase
{
    [HttpGet("state")]
    public IActionResult GetState() => Ok(service.GetState());

    [HttpPost("enroll")]
    public IActionResult Enroll([FromBody] EnrollmentRequest request)
    {
        if (request.LearnerId == Guid.Empty) return BadRequest(new { message = "LearnerId must be a non-empty GUID." });
        var result = service.Enroll(request.LearnerId);
        return result.Status switch
        {
            EnrollmentStatus.Enrolled => Ok(result),
            EnrollmentStatus.Waitlisted => StatusCode(StatusCodes.Status202Accepted, result),
            EnrollmentStatus.NotFound => NotFound(result),
            EnrollmentStatus.Rejected => UnprocessableEntity(result),
            _ => Conflict(result)
        };
    }

    [HttpPost("drop")]
    public IActionResult Drop([FromBody] EnrollmentRequest request)
    {
        if (request.LearnerId == Guid.Empty) return BadRequest(new { message = "LearnerId must be a non-empty GUID." });
        var result = service.Drop(request.LearnerId);
        return result.Status == EnrollmentStatus.NotFound ? NotFound(result) : Ok(result);
    }
}
