namespace CourseWaitlist.Api.Models;
public enum EnrollmentStatus { Enrolled, Waitlisted, Rejected, Dropped, NotFound, AlreadyEnrolled }
public sealed record EnrollmentResult(EnrollmentStatus Status, string Message, int EnrolledCount, int Capacity, Guid? PromotedLearnerId = null);
public sealed record EnrollmentRequest(Guid LearnerId);
public sealed record TokenRequest(string Username, string Password);
public sealed record TokenResponse(string AccessToken, string TokenType, int ExpiresInSeconds);
