using CourseWaitlist.Api.Models;
using CourseWaitlist.Api.Repositories;
namespace CourseWaitlist.Api.Services;

public sealed class EnrollmentService(IEnrollmentRepository repository) : IEnrollmentService
{
    private readonly object _gate = new();
    private const decimal MinimumScoreExclusive = 80m;

    public EnrollmentResult Enroll(Guid learnerId)
    {
        lock (_gate)
        {
            var learner = repository.FindLearner(learnerId);
            if (learner is null) return Result(EnrollmentStatus.NotFound, "Learner was not found.");
            if (repository.IsEnrolled(learnerId)) return Result(EnrollmentStatus.AlreadyEnrolled, "Learner is already enrolled.");
            if (repository.IsWaitlisted(learnerId)) return Result(EnrollmentStatus.Waitlisted, "Learner is already on the waitlist.");
            if (learner.Score <= MinimumScoreExclusive) return Result(EnrollmentStatus.Rejected, "Prerequisite failed: score must be greater than 80.");
            if (repository.Enrollments.Count < repository.Offering.Capacity)
            {
                repository.AddEnrollment(learnerId, DateTimeOffset.UtcNow);
                return Result(EnrollmentStatus.Enrolled, "Enrollment successful.");
            }
            repository.AddToWaitlist(learnerId);
            return Result(EnrollmentStatus.Waitlisted, "Course is full; learner added to the FIFO waitlist.");
        }
    }

    public EnrollmentResult Drop(Guid learnerId)
    {
        lock (_gate)
        {
            if (repository.FindLearner(learnerId) is null) return Result(EnrollmentStatus.NotFound, "Learner was not found.");
            if (!repository.RemoveEnrollment(learnerId)) return Result(EnrollmentStatus.Dropped, "Learner was not enrolled; no state changed.");
            Guid? promoted = null;
            // FIFO among eligible learners: ineligible entries are skipped but remain queued.
            foreach (var queuedId in repository.Waitlist.ToArray())
            {
                var candidate = repository.FindLearner(queuedId);
                if (candidate is null || candidate.Score <= MinimumScoreExclusive) continue;
                repository.RemoveFromWaitlist(queuedId);
                repository.AddEnrollment(queuedId, DateTimeOffset.UtcNow);
                promoted = queuedId;
                break;
            }
            return Result(EnrollmentStatus.Dropped, promoted is null ? "Enrollment dropped; no eligible waitlisted learner to promote." : "Enrollment dropped and next eligible learner promoted.", promoted);
        }
    }

    public object GetState() => new
    {
        repository.Offering,
        Learners = repository.Learners,
        Enrollments = repository.Enrollments,
        Waitlist = repository.Waitlist,
        Prerequisite = "score > 80"
    };

    private EnrollmentResult Result(EnrollmentStatus status, string message, Guid? promoted = null) =>
        new(status, message, repository.Enrollments.Count, repository.Offering.Capacity, promoted);
}
