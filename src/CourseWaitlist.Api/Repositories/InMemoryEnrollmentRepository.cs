using CourseWaitlist.Api.Models;
namespace CourseWaitlist.Api.Repositories;

public sealed class InMemoryEnrollmentRepository : IEnrollmentRepository
{
    private readonly Dictionary<Guid, Learner> _learners;
    private readonly List<Enrollment> _enrollments = [];
    private readonly List<Guid> _waitlist = [];
    public InMemoryEnrollmentRepository()
    {
        var learners = new[]
        {
            new Learner(Guid.Parse("10000000-0000-0000-0000-000000000001"), "Aarav", 95),
            new Learner(Guid.Parse("10000000-0000-0000-0000-000000000002"), "Diya", 91),
            new Learner(Guid.Parse("10000000-0000-0000-0000-000000000003"), "Kabir", 88),
            new Learner(Guid.Parse("10000000-0000-0000-0000-000000000004"), "Meera", 84),
            new Learner(Guid.Parse("10000000-0000-0000-0000-000000000005"), "Rohan", 79)
        };
        _learners = learners.ToDictionary(x => x.Id);
        Offering = new CourseOffering("CS101", "Introduction to Computer Science", 2);
    }
    public CourseOffering Offering { get; }
    public IReadOnlyCollection<Learner> Learners => _learners.Values.ToArray();
    public IReadOnlyCollection<Enrollment> Enrollments => _enrollments.ToArray();
    public IReadOnlyList<Guid> Waitlist => _waitlist.ToArray();
    public Learner? FindLearner(Guid id) => _learners.GetValueOrDefault(id);
    public bool IsEnrolled(Guid id) => _enrollments.Any(x => x.LearnerId == id);
    public bool IsWaitlisted(Guid id) => _waitlist.Contains(id);
    public void AddEnrollment(Guid id, DateTimeOffset at) => _enrollments.Add(new Enrollment(id, at));
    public bool RemoveEnrollment(Guid id) => _enrollments.RemoveAll(x => x.LearnerId == id) > 0;
    public bool AddToWaitlist(Guid id) { if (_waitlist.Contains(id)) return false; _waitlist.Add(id); return true; }
    public bool RemoveFromWaitlist(Guid id) => _waitlist.Remove(id);
}
