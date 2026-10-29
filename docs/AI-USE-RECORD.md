# AI-assisted engineering record

## Prompt used
“Create a .NET 10 solution with proper repository pattern but repository shouldn't connect to DB instead it should use in-memory data as per the document. Make sure proper models are used and pre-requisite will be a score of more than 80 for each of the learner which will be stored in memory. Add proper authorization filter with one endpoint to generate token.”

## AI contribution summary
- Proposed a layered API structure with models, repository interface/in-memory implementation, service workflow, JWT token endpoint, authorization on course endpoints, and focused unit tests.
- Interpreted “score of more than 80” strictly as `Score > 80`; score 80 is ineligible.
- Defined the brief's waitlist ambiguity as: skip ineligible queue entries during promotion while retaining them in place.

## Review / verification status
- Accepted the repository abstraction and separation of HTTP/controller, service, and storage responsibilities.
- Edited/limited scope to one offering, seeded in-memory data, JWT bearer authentication, and service-level locking for atomic transitions.
- Security caveat: token endpoint uses demo-only fixed credentials and a development key; these must be replaced/configured for production.
- Automated tests are included for capacity, full capacity, duplicate requests, FIFO promotion, prerequisite failure, unknown learner, repeated drop, and ineligible waitlist skipping.
- Run `dotnet test` locally to independently verify against the installed .NET 10 SDK. This artifact has not been executed in this generation environment; test execution remains to be confirmed by the recipient.

## Test strategy update: mocking
- The service tests use Moq with strict mocks for `IEnrollmentRepository`, so service behavior is tested without relying on the concrete in-memory repository implementation.
- The mock callbacks maintain small in-test lists to model repository state transitions and allow assertions on calls such as `AddEnrollment`, `AddToWaitlist`, and `RemoveFromWaitlist`.
- Verification: run `dotnet test` after restoring packages. Test execution has not been run in this generation environment.
