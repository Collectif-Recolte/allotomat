---
name: code-review-csharp
description: Reviews C# and .NET code for correctness, security, performance, and best practices. Use when the user asks for a C# code review, review of backend/API code, or when reviewing .cs files, pull requests in Sig.App.Backend, or GraphQL/mutation/query handlers.
---

# C# Code Review Agent

Specialized reviewer for C# and .NET code (e.g. Sig.App.Backend). Apply this skill when performing a code review on C# files, backend APIs, or GraphQL handlers.

## Scope

- **Files**: `**/*.cs` in backend (e.g. `Sig.App.Backend/**`), including requests, services, Gql schema, DbModel, helpers, background jobs.
- **Out of scope**: Frontend Vue/TS; this agent focuses only on C#.

## Review Checklist

### Correctness & Logic

- [ ] Business rules and edge cases are handled (nulls, empty collections, boundaries).
- [ ] Async/await is used correctly; no `.Result` or `.Wait()` on async code where it can deadlock.
- [ ] Database access: queries are efficient, N+1 avoided; consider `.AsNoTracking()` for read-only.
- [ ] Transactions used where multiple writes must be atomic.

### Security

- [ ] No sensitive data in logs or exceptions (PII, tokens, passwords).
- [ ] Authorization checked before any operation (user/role/scope); no trust of client-only checks.
- [ ] Input validation and sanitization; SQL/GraphQL injection vectors considered.
- [ ] File paths and user input not used unsanitized for file system or external calls.

### API & GraphQL

- [ ] Resolvers/mutations/queries are focused; heavy logic in services, not in schema.
- [ ] Errors returned in a consistent shape; avoid leaking stack traces or internal details.
- [ ] Pagination and filtering applied where lists can be large.

### .NET Conventions

- [ ] Naming: PascalCase for public members, _camelCase for private fields; meaningful names.
- [ ] Dependency injection: prefer constructor injection; avoid service locator.
- [ ] Disposal: `IDisposable`/`IAsyncDisposable` used and disposed correctly.
- [ ] Nullability: nullable reference types used consistently where applicable.

### Maintainability

- [ ] Single responsibility; no oversized classes or methods.
- [ ] Duplication extracted; shared logic in helpers/services.
- [ ] No magic numbers/strings; constants or config where appropriate.

## Feedback Format

Structure feedback so it’s easy to act on:

- **Critical**: Must fix before merge (bugs, security, data integrity).
- **Suggestion**: Should fix or strongly consider (performance, correctness edge cases).
- **Nice to have**: Style, clarity, small refactors.

For each point: **location** (file/method), **issue**, **suggestion** (with code snippet when helpful).

## Example Snippet

```csharp
// ❌ Avoid: blocking on async
var result = GetDataAsync().Result;

// ✅ Prefer: async all the way
var result = await GetDataAsync();
```

```csharp
// ❌ Avoid: N+1 in loop
foreach (var item in items)
    item.Detail = await _context.Details.FindAsync(item.DetailId);

// ✅ Prefer: batch load
var ids = items.Select(i => i.DetailId).Distinct().ToList();
var details = await _context.Details.Where(d => ids.Contains(d.Id)).ToDictionaryAsync(d => d.Id);
foreach (var item in items)
    item.Detail = details.GetValueOrDefault(item.DetailId);
```

Keep the review concise: prioritize critical and suggestion items; nice-to-haves can be a short list.
