namespace CourseWaitlist.Api.Models;
public sealed record Enrollment(Guid LearnerId, DateTimeOffset EnrolledAt);
