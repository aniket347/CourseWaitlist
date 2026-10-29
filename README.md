# Course Waitlist API (.NET 10)

A bounded course enrollment, drop, and FIFO waitlist-promotion API based on the supplied assessment brief. State is held only in an in-memory repository; there is no database.

## Prerequisites and policy
- Target framework: .NET 10 SDK.
- Learner eligibility is **score > 80** (score 80 is not eligible).
- Seed data includes four learners above 80 and one below 80. Offering `CS101` has capacity 2.
- An ineligible enrollment request is rejected and is not added to the waitlist.
- Waitlist promotion scans FIFO order and promotes the first eligible learner. Ineligible entries are skipped but retained in their original queue position.
- Duplicate enrollment/waitlist requests do not create duplicate records.
- A lock in the service makes enrollment/drop state transitions atomic within this single process. In-memory state resets on restart and is not shared across multiple instances.

## Run
```bash
dotnet restore
dotnet build
dotnet test
dotnet run --project src/CourseWaitlist.Api
```
The API prints its local URL when started. Open `/swagger` on that host to use Swagger UI and explore the endpoints. Swagger includes a JWT Bearer security scheme: generate a token with `POST /api/auth/token`, click **Authorize**, and paste the token (or `Bearer <token>` if prompted).

## Authentication
Request a demo JWT:
```bash
curl -X POST http://localhost:5000/api/auth/token -H "Content-Type: application/json" -d "{\"username\":\"course-admin\",\"password\":\"ChangeMe123!\"}"
```
Use the returned `accessToken` as `Authorization: Bearer <token>` for protected endpoints. Exact port/scheme depends on launch settings/environment.

**Security warning:** demo credentials and the development signing key are intentionally local placeholders. Configure a strong secret through configuration/environment variables and replace demo credential validation with a proper identity provider before production. Never commit real secrets.

## Endpoints
- `POST /api/auth/token` — issue JWT for demo credentials.
- `GET /api/course/state` — inspect offering, seeded learners, enrolled learners, waitlist and prerequisite (JWT required).
- `POST /api/course/enroll` — body `{ "learnerId": "<guid>" }` (JWT required).
- `POST /api/course/drop` — body `{ "learnerId": "<guid>" }` (JWT required).

Seed learner IDs:
| Learner | ID | Score |
|---|---|---:|
| Aarav | `10000000-0000-0000-0000-000000000001` | 95 |
| Diya | `10000000-0000-0000-0000-000000000002` | 91 |
| Kabir | `10000000-0000-0000-0000-000000000003` | 88 |
| Meera | `10000000-0000-0000-0000-000000000004` | 84 |
| Rohan | `10000000-0000-0000-0000-000000000005` | 79 |

## Design notes
- Controllers handle HTTP validation/status codes; `EnrollmentService` owns workflow and invariants; `IEnrollmentRepository` abstracts storage; `InMemoryEnrollmentRepository` owns in-memory data.
- The repository does not open connections or perform I/O.
- `EnrollmentResult` returns stable domain status/message/count/capacity information.
- Duplicate drops are idempotent: a repeated drop reports no state change.
- The supplied brief requests focused tests and an AI-use record; see `docs/AI-USE-RECORD.md`.

## Limitations / follow-up risks
- Demo token endpoint uses hard-coded credentials, not suitable for production.
- In-memory state is process-local and non-durable.
- No refresh/revocation, rate limiting, audit trail, database, or multi-instance coordination.
- Tests use Moq for `IEnrollmentRepository` and exercise service logic, not full HTTP/JWT integration.
