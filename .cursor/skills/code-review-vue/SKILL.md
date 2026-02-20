---
name: code-review-vue
description: Reviews Vue.js and frontend code for correctness, accessibility, performance, and best practices. Use when the user asks for a Vue code review, frontend review, or when reviewing .vue files, components in Sig.App.Frontend, or Vue/Composition API usage.
---

# Vue.js Code Review Agent

Specialized reviewer for Vue.js and frontend code (e.g. Sig.App.Frontend). Apply when performing a code review on `.vue` files, components, views, or frontend logic.

## Scope

- **Files**: `**/*.vue`, and related `**/*.ts`/`**/*.js` in frontend (e.g. `Sig.App.Frontend/**`).
- **Stack**: Vue (Options or Composition API), TypeScript/JavaScript, Vue Router, i18n, Tailwind/pinkflamant.
- **Out of scope**: Backend C#; this agent focuses on frontend only.

## Review Checklist

### Correctness & Logic

- [ ] Reactive data used correctly; no accidental mutations of props; `v-model` and events align with parent/child contract.
- [ ] Async/loading states handled (loading flags, error messages, empty states).
- [ ] Route params/query and form state stay in sync; no stale data after navigation or refresh when it matters.
- [ ] Event handlers and API calls are wired to the right scope (no missing `this` or closure bugs in Composition API).

### Vue Patterns

- [ ] Components are focused: presentational vs container split where it helps; no oversized single-file components.
- [ ] Props typed and validated where applicable; avoid optional props that are effectively required without defaults.
- [ ] Emits declared and named clearly; payloads minimal and documented if non-obvious.
- [ ] Composables used for shared logic; no duplicated logic across components when a composable fits.
- [ ] Keys used correctly in `v-for` (stable, unique); avoid index as key when list can change.

### Template & Accessibility

- [ ] Semantic HTML where possible (buttons for actions, links for navigation, labels for inputs).
- [ ] Forms: labels associated with inputs; errors announced (e.g. `aria-describedby`, `role="alert"`).
- [ ] Interactive elements keyboard-accessible; focus management in modals/dialogs if present.
- [ ] i18n: user-facing strings go through `t()` (or project i18n helper); no hardcoded copy in template/script.

### Performance & DX

- [ ] No unnecessary re-renders: avoid inline object/array creation in template (e.g. `:style="{ ... }"` recreated every tick); consider computed or stable refs.
- [ ] Heavy lists: consider virtualization or pagination if the list can be large.
- [ ] Assets: images/icons referenced appropriately; lazy loading where it helps.
- [ ] No `v-if`/`v-for` on the same element (Vue discourages); use wrapper or computed filtered list.

### Security & Data

- [ ] User input not interpolated as HTML without sanitization (`v-html` only with trusted/sanitized content).
- [ ] Sensitive data not logged or exposed in client-visible errors.
- [ ] API/base URLs and feature flags from config/env, not hardcoded.

### Project Conventions (Sig.App.Frontend)

- [ ] Naming: kebab-case for component files and multi-word components; PascalCase for component usage in template.
- [ ] Pinkflamant/design system components used consistently (e.g. PfButtonLink, form inputs) where applicable.
- [ ] Styles: Tailwind/pinkflamant classes preferred; scoped styles if custom CSS needed; no global leaks unless intentional.

## Feedback Format

- **Critical**: Must fix before merge (bugs, accessibility blockers, security, data loss risk).
- **Suggestion**: Should fix or strongly consider (UX, performance, maintainability).
- **Nice to have**: Style, consistency, small refactors.

For each point: **location** (file/component/section), **issue**, **suggestion** (with code snippet when helpful).

## Example Snippets

```vue
<!-- ❌ Avoid: v-if and v-for on same element -->
<li v-for="item in items" v-if="item.visible" :key="item.id">

<!-- ✅ Prefer: filter in computed or wrap -->
<li v-for="item in visibleItems" :key="item.id">
```

```vue
<!-- ❌ Avoid: inline object causing repeated re-renders -->
<Child :config="{ mode: 'edit' }" />

<!-- ✅ Prefer: stable reference -->
<Child :config="editConfig" />
```

Keep the review concise; prioritize critical and suggestion items.
