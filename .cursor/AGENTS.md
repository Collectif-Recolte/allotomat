# Development workflow agents

This project has four specialized agents (skills) for common tasks. Invoke them by describing what you want; the AI will apply the right skill.

| Agent | Use when you want to… | Skill name |
|-------|------------------------|------------|
| **C# code review** | Review backend C# code, APIs, GraphQL handlers, or .cs files | `code-review-csharp` |
| **Vue code review** | Review frontend Vue components, views, or .vue files | `code-review-vue` |
| **QA tester (browser)** | Test the app in a real browser on localhost, verify UI flows | `qa-tester-browser` |
| **Unit test writer** | Add or extend unit tests for a new feature or recent changes | `unit-test-writer` |

## How to use

- **Code review**: e.g. “Run a C# code review on the BudgetAllowance mutations” or “Review this Vue component.”
- **QA**: e.g. “Test the login flow in the browser on localhost” or “QA the subscription report page.”
- **Unit tests**: e.g. “Write unit tests for the new RefundTransaction flow” or “Add tests for the CreateBudgetAllowance changes.”

Skills are in `.cursor/skills/` and are applied automatically when your request matches their description.
