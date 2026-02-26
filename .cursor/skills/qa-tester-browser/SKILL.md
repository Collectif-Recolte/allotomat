---
name: qa-tester-browser
description: Performs QA testing of web applications in a local browser using MCP browser tools. Use when the user wants to test the app in the browser, run QA checks locally, verify UI flows, or validate frontend behavior on localhost.
---

# QA Tester (Browser) Agent

Performs QA testing of the web app by controlling a browser (e.g. local dev server). Use the cursor-ide-browser MCP: navigate, snapshot, click, type, and assert content. Apply when the user asks to test the app in the browser, run QA, or verify flows on localhost.

## When to Use

- User says: "test the app in the browser", "QA the login flow", "check that X works on localhost", "verify the UI for Y".
- Goal: exercise real UI in a browser and report pass/fail and issues.

## Prerequisites

1. **Dev server running**: Frontend (and backend if needed) must be running (e.g. `npm run dev`, backend on its port). If unsure, ask the user or try starting it.
2. **Base URL**: Prefer localhost (e.g. `http://localhost:5173` or the port the user specifies). Confirm with user if multiple apps/ports.

## Browser Workflow (MCP)

1. **List tabs**: `browser_tabs` with action `list` to see existing tabs and URLs.
2. **Navigate**: `browser_navigate` to the target URL (e.g. login page).
3. **Lock**: `browser_lock` **after** a tab exists (after navigate). Cannot lock before navigate.
4. **Snapshot**: `browser_snapshot` to get page structure and element refs before any click/type/hover.
5. **Interact**: Use refs from snapshot for `browser_click`, `browser_type`, `browser_fill`, `browser_hover`, etc. Use `browser_fill` to clear and replace; `browser_type` to append.
6. **Scroll**: For nested scroll containers, use `browser_scroll` with `scrollIntoView: true` before clicking obscured elements.
7. **Wait**: Prefer short waits (1–3 s) then snapshot again to see if content loaded, rather than one long wait.
8. **Unlock**: `browser_unlock` when **all** browser operations for the turn are done.

Order: **navigate → lock → (snapshots + interactions) → unlock**.

## Testing Approach

1. **Scope**: Clarify with user what to test (e.g. "login", "create project", "report X") and which URL to use.
2. **Steps**: Break into clear steps (e.g. open login → fill email → fill password → submit → check redirect).
3. **Assert**: After each important action, snapshot and confirm expected content (e.g. URL change, heading text, error message).
4. **Report**: Summarize what was tested, what passed, what failed, and any bugs (screenshot refs or element text).

## Dialogs

- Native `alert`/`confirm`/`prompt` do not block automation. By default `confirm()` is accepted, `prompt()` returns default.
- To test "Cancel" or custom prompt: call `browser_handle_dialog` **before** the action that opens the dialog (e.g. `accept: false` for Cancel).

## Limitations

- Iframe content is not accessible; only top-frame elements can be used.
- Prefer incremental waits + snapshots over one long wait so you can proceed as soon as the page is ready.

## Example QA Report Format

```markdown
# QA: [Feature/flow name]

**URL**: http://localhost:5173/...
**Steps**: 1) … 2) …

## Results
- [ ] Step 1: …
- [ ] Step 2: …
- [ ] Step 3: …

## Issues
- (none | list of failures and observed behavior)

## Notes
- (browser, viewport, or env notes if relevant)
```

Keep tests focused and report clearly so the user can fix any failures.
