# Revit Model Audit Add-in Handbook

# 1. Project Overview

This project is a Revit add-in that audits BIM models and produces a numbered report of model health and compliance.

## Current Goals
- Audit Project Information
- Audit Shared Parameters
- Audit Splash Screen
- Audit Project Base Point and coordinate synchronization
- Produce human-readable audit reports
- Keep the code modular and reusable

---

# 2. Architecture

## Reporting Categories

### Compliance
Uses `AddInformation()` to answer a business question with a pass/fail result.

### Inventory
Uses `AddInventory()` to list model content (e.g. linked DWGs) without assigning pass/fail.

### Metrics
Reports counts and measurements.

Design principle: separate **data collection** (FilteredElementCollector) from **presentation** (report helpers).

---

# 3. Coding Standards

- Prefer helper methods.
- Separate retrieval from formatting.
- Use `?.` for optional parameters.
- Use `string.IsNullOrWhiteSpace()`.
- Keep methods focused on a single responsibility.

---

# 4. Reporting Framework

## AddInformation()

Use for single compliance checks.

## AddInventory()

Use for inventories:
- Group duplicates with `GroupBy()`
- Show total instances
- Show duplicate counts

---

# 5. Audit Patterns

- General Information
- Splash Screen
- Coordinate Synchronization (Project Base Point vs shared parameters)
- Future audit methods per section

---

# 6. Revit API Recipes

- LookupParameter()
- FilteredElementCollector
- ViewSheet
- ImportInstance (linked DWGs)
- BasePoint
- LINQ: FirstOrDefault(), Any(), GroupBy()

---

# 7. Troubleshooting

- Restart ArchSmarter if 'One or more errors occurred' appears unexpectedly.
- During development, wrap each audit section in try/catch.

---

# 8. Design Decisions

- One audit should answer one business question.
- Inventories are informational, not pass/fail.
- Project Base Point is the source of truth for coordinate synchronization.
- Separate collection from presentation.

---

# 9. Project Roadmap

## Planned
- Refactor each audit section into methods.
- Add per-section try/catch.
- Expand Project Base Point audit.
- Expand inventory reporting.

---

# 10. Changelog



## 2026-07-08

### Added

-   Created the initial Revit Model Audit Add-in Handbook.
-   Introduced the `AddInformation()` helper method pattern to reduce
    duplicated code.
-   Explained the `ref` keyword for automatic subsection numbering.
-   Explained the null-conditional operator (`?.`) and how it prevents
    `NullReferenceException`.
-   Reviewed the splash screen audit workflow using
    `FilteredElementCollector`, `ViewSheet`, `FirstOrDefault()`, and
    `Any()`.
-   Corrected the `ViewSheet` type name (`ViewSheet` vs `Viewsheet`).

### Documentation Practice

-   Maintain this handbook as the single source of project knowledge.
-   Update the handbook at meaningful milestones or after significant
    refactoring.
-   Record important architectural decisions and reusable code patterns.

## 2026-07-09

### Added

- Documented Splash Screen audit implementation.
- Added explanation of short-circuit evaluation (`&&`).
- Added explanation of the ternary operator (`?:`).
- Added Project Base Point extraction notes.
- Corrected `BuiltInCategory.OST_TitleBlocks`.

### Troubleshooting

- Recorded intermittent "One or more errors occurred" issue.
- Identified that restarting ArchSmarter resolved the problem.

### Planned

- Refactor audits into individual methods.
- Add per-audit exception handling.

# Project Roadmap

## ✅ Completed

- Created reusable `AddInformation()` helper method.
- Implemented automatic section and subsection numbering.
- Audited General Information.
- Implemented Splash Screen audit.
- Documented common Revit API patterns.
- Documented helper method design.
- Added project changelog.

## 🟡 In Progress

- Project Base Point audit.
- Survey Point audit.
- Model information expansion.

## 🔵 Planned

- Refactor each audit section into its own method.
- Add per-section `try/catch` blocks during development.
- Introduce logging for unexpected exceptions.
- Reduce duplicated code across audit sections.

## ⚪ Future Ideas

- HTML report generation.
- PDF report generation.
- Excel export.
- Configuration-driven audit rules.
- User-configurable audit settings.
- Unit tests for helper methods.

# Troubleshooting

## "One or more errors occurred"

### Symptoms

- Generic runtime error.
- No obvious indication of the failing audit.
- Code may have worked previously.

### Possible Causes

- Stale ArchSmarter session.
- Cached assemblies.
- Revit API context not fully reset.
- AggregateException masking the underlying exception.

### Resolution

1. Restart the ArchSmarter scripting environment.
2. Restart Revit if required.
3. Re-run the audit.
4. Inspect the InnerException if the error persists.

### Recommendation

During development, wrap each audit section in a `try/catch` block to quickly identify the failing section.

# Outstanding Improvements

## High Priority

- Refactor each audit section into its own method.
- Add `try/catch` blocks around each audit section.
- Improve diagnostic messages.

## Medium Priority

- Create helper methods for commonly repeated Revit API queries.
- Reduce repeated parameter lookup logic.

## Low Priority

- Introduce configuration file for audit settings.
- Improve report formatting.

# Design Decisions

## Helper Method: AddInformation()

Reason:
Centralize report formatting and automatic subsection numbering.

Benefits:
- Eliminates duplicate code.
- Ensures consistent output.
- Simplifies future formatting changes.

---

## Audit Structure

Decision:
Group related checks into dedicated audit methods.

Reason:
Improves readability, maintainability, and debugging.

Status:
Planned.

---

## Reporting Philosophy

Decision:
Return human-readable values rather than raw API values where possible.

Example:
Use `Parameter.AsValueString()` for audit reports when matching the Revit UI is preferable to displaying internal units.

