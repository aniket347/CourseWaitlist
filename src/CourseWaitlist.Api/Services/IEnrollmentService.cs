using CourseWaitlist.Api.Models;
namespace CourseWaitlist.Api.Services;
public interface IEnrollmentService
{
    EnrollmentResult Enroll(Guid learnerId);
    EnrollmentResult Drop(Guid learnerId);
    object GetState();
}
