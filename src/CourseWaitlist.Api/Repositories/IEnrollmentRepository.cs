using CourseWaitlist.Api.Models;
namespace CourseWaitlist.Api.Repositories;

// Repository owns in-memory state only. No database or external persistence is used.
public interface IEnrollmentRepository
{
    CourseOffering Offering { get; }
    IReadOnlyCollection<Learner> Learners { get; }
    IReadOnlyCollection<Enrollment> Enrollments { get; }
    IReadOnlyList<Guid> Waitlist { get; }
    Learner? FindLearner(Guid id);
    bool IsEnrolled(Guid learnerId);
    bool IsWaitlisted(Guid learnerId);
    void AddEnrollment(Guid learnerId, DateTimeOffset at);
    bool RemoveEnrollment(Guid learnerId);
    bool AddToWaitlist(Guid learnerId);
    bool RemoveFromWaitlist(Guid learnerId);
}
