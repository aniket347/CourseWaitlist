using CourseWaitlist.Api.Models;
using CourseWaitlist.Api.Repositories;
using CourseWaitlist.Api.Services;
using Moq;
using Xunit;

namespace CourseWaitlist.Tests;

public sealed class EnrollmentServiceTests
{
    private static readonly Guid Aarav = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid Diya = Guid.Parse("10000000-0000-0000-0000-000000000002");
    private static readonly Guid Kabir = Guid.Parse("10000000-0000-0000-0000-000000000003");
    private static readonly Guid Meera = Guid.Parse("10000000-0000-0000-0000-000000000004");
    private static readonly Guid Rohan = Guid.Parse("10000000-0000-0000-0000-000000000005");

    private static Mock<IEnrollmentRepository> CreateRepository(
        int capacity = 2,
        List<Enrollment>? enrollments = null,
        List<Guid>? waitlist = null,
        Learner[]? learners = null)
    {
        enrollments ??= [];
        waitlist ??= [];
        learners ??=
        [
            new Learner(Aarav, "Aarav", 95),
            new Learner(Diya, "Diya", 91),
            new Learner(Kabir, "Kabir", 88),
            new Learner(Meera, "Meera", 84),
            new Learner(Rohan, "Rohan", 79)
        ];

        var mock = new Mock<IEnrollmentRepository>(MockBehavior.Strict);
        mock.SetupGet(r => r.Offering).Returns(new CourseOffering("CS101", "Introduction to Computer Science", capacity));
        mock.SetupGet(r => r.Learners).Returns(() => learners);
        mock.SetupGet(r => r.Enrollments).Returns(() => enrollments.ToArray());
        mock.SetupGet(r => r.Waitlist).Returns(() => waitlist.ToArray());
        mock.Setup(r => r.FindLearner(It.IsAny<Guid>())).Returns((Guid id) => learners.SingleOrDefault(x => x.Id == id));
        mock.Setup(r => r.IsEnrolled(It.IsAny<Guid>())).Returns((Guid id) => enrollments.Any(x => x.LearnerId == id));
        mock.Setup(r => r.IsWaitlisted(It.IsAny<Guid>())).Returns((Guid id) => waitlist.Contains(id));
        mock.Setup(r => r.AddEnrollment(It.IsAny<Guid>(), It.IsAny<DateTimeOffset>()))
            .Callback<Guid, DateTimeOffset>((id, at) => enrollments.Add(new Enrollment(id, at)));
        mock.Setup(r => r.RemoveEnrollment(It.IsAny<Guid>()))
            .Returns((Guid id) => enrollments.RemoveAll(x => x.LearnerId == id) > 0);
        mock.Setup(r => r.AddToWaitlist(It.IsAny<Guid>()))
            .Returns((Guid id) => { if (waitlist.Contains(id)) return false; waitlist.Add(id); return true; });
        mock.Setup(r => r.RemoveFromWaitlist(It.IsAny<Guid>()))
            .Returns((Guid id) => waitlist.Remove(id));
        return mock;
    }

    [Fact]
    public void EnrollsWhenCapacityIsAvailable()
    {
        var repository = CreateRepository();
        var service = new EnrollmentService(repository.Object);

        var result = service.Enroll(Aarav);

        Assert.Equal(EnrollmentStatus.Enrolled, result.Status);
        Assert.Contains(repository.Object.Enrollments, e => e.LearnerId == Aarav);
        repository.Verify(r => r.AddEnrollment(Aarav, It.IsAny<DateTimeOffset>()), Times.Once);
    }

    [Fact]
    public void AddsEligibleLearnerToWaitlistWhenCourseIsFull()
    {
        var enrollments = new List<Enrollment>
        {
            new(Aarav, DateTimeOffset.UtcNow),
            new(Diya, DateTimeOffset.UtcNow)
        };
        var repository = CreateRepository(enrollments: enrollments);
        var service = new EnrollmentService(repository.Object);

        var result = service.Enroll(Kabir);

        Assert.Equal(EnrollmentStatus.Waitlisted, result.Status);
        Assert.Equal(new[] { Kabir }, repository.Object.Waitlist);
        repository.Verify(r => r.AddToWaitlist(Kabir), Times.Once);
    }

    [Fact]
    public void DuplicateEnrollmentDoesNotCreateAnotherRecord()
    {
        var enrollments = new List<Enrollment> { new(Aarav, DateTimeOffset.UtcNow) };
        var repository = CreateRepository(enrollments: enrollments);
        var service = new EnrollmentService(repository.Object);

        var result = service.Enroll(Aarav);

        Assert.Equal(EnrollmentStatus.AlreadyEnrolled, result.Status);
        Assert.Single(repository.Object.Enrollments);
        repository.Verify(r => r.AddEnrollment(It.IsAny<Guid>(), It.IsAny<DateTimeOffset>()), Times.Never);
    }

    [Fact]
    public void DuplicateWaitlistRequestDoesNotAddDuplicate()
    {
        var enrollments = new List<Enrollment>
        {
            new(Aarav, DateTimeOffset.UtcNow),
            new(Diya, DateTimeOffset.UtcNow)
        };
        var waitlist = new List<Guid> { Kabir };
        var repository = CreateRepository(enrollments: enrollments, waitlist: waitlist);
        var service = new EnrollmentService(repository.Object);

        var result = service.Enroll(Kabir);

        Assert.Equal(EnrollmentStatus.Waitlisted, result.Status);
        Assert.Single(repository.Object.Waitlist);
        repository.Verify(r => r.AddToWaitlist(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public void RejectsLearnerWhoseScoreIsNotGreaterThanEighty()
    {
        var repository = CreateRepository();
        var service = new EnrollmentService(repository.Object);

        var result = service.Enroll(Rohan);

        Assert.Equal(EnrollmentStatus.Rejected, result.Status);
        Assert.Empty(repository.Object.Enrollments);
        Assert.Empty(repository.Object.Waitlist);
        repository.Verify(r => r.AddEnrollment(It.IsAny<Guid>(), It.IsAny<DateTimeOffset>()), Times.Never);
        repository.Verify(r => r.AddToWaitlist(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public void UnknownLearnerIsNotEnrolled()
    {
        var repository = CreateRepository();
        var service = new EnrollmentService(repository.Object);

        var result = service.Enroll(Guid.NewGuid());

        Assert.Equal(EnrollmentStatus.NotFound, result.Status);
        repository.Verify(r => r.AddEnrollment(It.IsAny<Guid>(), It.IsAny<DateTimeOffset>()), Times.Never);
    }

    [Fact]
    public void DroppingEnrollmentPromotesEarliestEligibleWaitlistedLearner()
    {
        var enrollments = new List<Enrollment>
        {
            new(Aarav, DateTimeOffset.UtcNow),
            new(Diya, DateTimeOffset.UtcNow)
        };
        var waitlist = new List<Guid> { Kabir, Meera };
        var repository = CreateRepository(enrollments: enrollments, waitlist: waitlist);
        var service = new EnrollmentService(repository.Object);

        var result = service.Drop(Aarav);

        Assert.Equal(EnrollmentStatus.Dropped, result.Status);
        Assert.Equal(Kabir, result.PromotedLearnerId);
        Assert.Contains(repository.Object.Enrollments, e => e.LearnerId == Kabir);
        Assert.Equal(new[] { Meera }, repository.Object.Waitlist);
        repository.Verify(r => r.RemoveEnrollment(Aarav), Times.Once);
        repository.Verify(r => r.RemoveFromWaitlist(Kabir), Times.Once);
        repository.Verify(r => r.AddEnrollment(Kabir, It.IsAny<DateTimeOffset>()), Times.Once);
    }

    [Fact]
    public void SkipsButRetainsIneligibleLearnerAtFrontOfWaitlist()
    {
        var enrollments = new List<Enrollment>
        {
            new(Aarav, DateTimeOffset.UtcNow),
            new(Diya, DateTimeOffset.UtcNow)
        };
        var waitlist = new List<Guid> { Rohan, Kabir };
        var repository = CreateRepository(enrollments: enrollments, waitlist: waitlist);
        var service = new EnrollmentService(repository.Object);

        var result = service.Drop(Aarav);

        Assert.Equal(Kabir, result.PromotedLearnerId);
        Assert.Contains(Rohan, repository.Object.Waitlist);
        Assert.DoesNotContain(Kabir, repository.Object.Waitlist);
        Assert.Contains(repository.Object.Enrollments, e => e.LearnerId == Kabir);
        repository.Verify(r => r.RemoveFromWaitlist(Rohan), Times.Never);
    }

    [Fact]
    public void RepeatedDropDoesNotRemoveOtherEnrollmentOrPromoteWaitlist()
    {
        var enrollments = new List<Enrollment> { new(Diya, DateTimeOffset.UtcNow) };
        var waitlist = new List<Guid> { Kabir };
        var repository = CreateRepository(enrollments: enrollments, waitlist: waitlist);
        var service = new EnrollmentService(repository.Object);

        var result = service.Drop(Aarav);

        Assert.Equal(EnrollmentStatus.Dropped, result.Status);
        Assert.Contains(repository.Object.Enrollments, e => e.LearnerId == Diya);
        Assert.Equal(new[] { Kabir }, repository.Object.Waitlist);
        repository.Verify(r => r.AddEnrollment(Kabir, It.IsAny<DateTimeOffset>()), Times.Never);
    }

    [Fact]
    public void UnknownLearnerDropReturnsNotFound()
    {
        var repository = CreateRepository();
        var service = new EnrollmentService(repository.Object);

        var result = service.Drop(Guid.NewGuid());

        Assert.Equal(EnrollmentStatus.NotFound, result.Status);
        repository.Verify(r => r.RemoveEnrollment(It.IsAny<Guid>()), Times.Never);
    }
}
