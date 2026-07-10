# Revit 2024 Model Group Type Name Migration -- Revised Project Recap

## Project Goal

Develop a lean, one-time ArchSmarter Launchpad C# script to migrate
Model Group Type names from an older RVT model (source) to a newer copy
(target).

## Platform

-   ArchSmarter Launchpad 1.5.1.0
-   Native Launchpad C# script
-   No namespace
-   No IExternalCommand
-   No Execute()
-   No .addin manifest

## Final Design

-   Operate on GroupType elements, not Group instances.
-   Match by GroupType.UniqueId.
-   Filter prefixes:
    -   AAI_Apartment
    -   AAI_Bathroom
    -   AAI_Kitchen

## TSV Format

Columns: - UniqueId - ElementId - GroupName - Category

## Workflow

1.  Export Group Types
2.  Read TSV
3.  Build RenamePlan
4.  Validate
5.  Preview
6.  Backup target names
7.  Two-pass rename
8.  Verify
9.  Write audit report

## Data Structures

-   GroupTypeRecord
-   RenameItem
-   RenameConflict
-   RenamePlan

## Validation

-   Duplicate UniqueIds
-   Duplicate destination names
-   Missing matches
-   Already-correct names
-   Rename conflicts

## Rename Strategy

Use a two-pass rename: 1. Rename to temporary GUID-based names. 2.
Rename to final names.

## Reporting

Include: - To Rename - Already Correct - No Match - Duplicate IDs -
Duplicate destination names - Successful renames - Failed renames

## Dry Run

Support a DryRun mode to validate without modifying the model.

## Script Structure

1.  Configuration
2.  Data Classes
3.  Main Execution
4.  Export
5.  Import
6.  Validation
7.  Rename
8.  Reporting
9.  Helpers

## Lessons Learned

The original design targeted a compiled Revit add-in. After confirming
the execution environment is Launchpad, the implementation will be
rewritten as a native Launchpad script while keeping the migration
architecture.

## Next Steps

Implement a single Launchpad-native C# script (\~300--400 lines), test
on copied models, then run on the production target after verification.
