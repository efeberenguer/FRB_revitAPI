```
═══════════════════════════════════════════════════════════════
1. GENERAL INFORMATION
═══════════════════════════════════════════════════════════════

─────────────────
1.1 Model auditor: 
─────────────────
Francisco Berenguer - fberenguer@adamson-associates.com

─────────────────
1.2 Project number
──────────────────
2139
(FRB: Read only: The assumption is that this parameter will always be correctly populated. I am not sure if we need to account for an error message if it hasn't been populated)

────────────────
1.3 Project name
────────────────
NEXUS LONDON
(FRB: Read only: The assumption is that this parameter will always be correctly populated. I am not sure if we need to account for an error message if it hasn't been populated)

───────────────────────────
1.4 Project BIM Coordinator
───────────────────────────
Francisco Berenguer - fberenguer@adamson-associates.com
(FRB: Read only: The assumption is that this parameter will always be correctly populated. I am not sure if we need to account for an error message if it hasn't been populated)

───────────────────
1.5 Project Manager
───────────────────
Chris Oakley - COakley@adamson-associates.com
(FRB: we will need a parameter -AAI_ProjectManager- to extract this information, which could be added to the splash screen)

───────────────────
1.6 Model file name
───────────────────
EST-AAI-ZZ-ZZ-M3-A-00002
(FRB: Read only: The assumption is that this parameter will always be correctly populated. I am not sure if we need to account for an error message if it hasn't been populated)

─────────────────────
1.7 Model description
─────────────────────
Interior
(FRB: Read only: The assumption is that this parameter will always be correctly populated. I am not sure if we need to account for an error message if it hasn't been populated)

─────────────
1.8 File size
─────────────
124 MB
(FRB: It will require saving a detached copy of the model to extract this information)

──────────────
1.9 Issue date
──────────────
2026-08-21
// FRB: Read only: The assumption is that this parameter will always be correctly populated. I am not sure if we need to account for an error message if it hasn't been populated

═══════════════════════════════════════════════════════════════
2. AAI STANDARDS
═══════════════════════════════════════════════════════════════

────────────────
2.1A Linked DWGs
────────────────

// Scenario 1: No linked DWGs in the model

Model requirements
──────────────────
- RVT models may have one or more linked DWGs. 
- Linked DWGs must not have more than one instance.

Audit results
─────────────
- Linked DWGs types in the model: 0

Action required
───────────────
- None.

// Scenario 2: Linked DWGS in the model, single instances

Model requirements
──────────────────
- RVT models may have one or more linked DWGs. 
- Linked DWGs must not have more than one instance.

Audit results
─────────────
- Linked DWGs types in the model: 3
    1. EST-AC Level 22.dwg
    2. EST-AC Level MR.dwg
    3. Level 02.dwg
- Redundant instances of linked DWGs: 0

Model requirements
──────────────────
- Project team members should review the linked DWGs in the model and confirm if there are types that must be removed.

// Scenario 3: Linked DWGS, multiple instances

Model requirements
──────────────────
- RVT models may have one or more linked DWGs. 
- Linked DWGs must not have more than one instance.

Audit results
─────────────
- Linked DWGs types in the model: 3
    1. EST-AC Level 22.dwg (multiple instances)
    2. EST-AC Level MR.dwg
    3. Level 02.dwg (multiple instances)
- Redundant instances of linked DWGs: 5

Action required
─────────────── 
- Redundant instances of linked DWGs must be deleted.
- EST-AC Level 22.dwg | Instance(s) to delete: 1
    1. 1242178
    2. 1242180
- Level 02.dwg | Instance(s) to delete: 2
    1. 1243692
    2. 1243693
    3. 1243696

──────────────────
2.1B Imported DWGs
──────────────────

// Scenario 1: No imported DWGs in the model

Model requirements
──────────────────
- RVT models must not have imported DWG files.

Audit results
─────────────
- Imported DWGs types in the model: 0

Action required
───────────────
- None.

// Scenario 2: One or more imported DWGS in the model

Model requirements
──────────────────
- RVT models must not have imported DWG files

Audit results
─────────────
- Imported DWGs types in the model: 3
    1. EST-AC Level 22.dwg
    2. EST-AC Level MR.dwg
    3. Level 02.dwg

Action required
───────────────
- All imported DWGs must be removed from the model.
    1. EST-AC Level 22.dwg (1243692)
    2. EST-AC Level MR.dwg (1243694)
    3. Level 02.dwg (1243696)

───────────────────────────
2.2A Filled regions - Types
───────────────────────────

// The scope of the search in this section of the audit is limited to views (plans, RCPS, elevations, sections) within documentation sheets

// Scenario 1: No filled region within the documentation views

Model requirements
──────────────────
- Documentation views within RVT models must only use the standard filled regions types.
- Standard filled region types must not be modified. -

Audit results
─────────────
- Filled regions within documentation views: 0
- Modified standard filled region types: N/A
- Non-standard filled region types: N/A

Action required
───────────────
- None.

// Scenario 2: No non-standard filled region types within the documentation views

Model requirements
──────────────────
- Documentation views within RVT models must only use the standard filled regions types.
- Standard filled region types must not be modified. 

Audit results
─────────────
- Filled regions within documentation views: 123
- Modified standard filled region types: 0
- Non-standard filled region types: 0

Action required
───────────────
- None.

// Scenario 3: At least either one or more modified standard filled region or one or more non-standard filled region types within the documentation views

Model requirements
──────────────────
- Documentation views within RVT models must only use the standard filled regions types.
- Standard filled region types must not be modified. 

Audit results
─────────────
- Filled regions within documentation views: 123
- Modified standard filled region types:
    1. AAI_Concrete
    2. AAI_Gravel
- Non-standard filled region types
    1. AAI_Solid_WhiteO
    2. AAI_Solid_WhiteT

Action required
───────────────
- All modified standard filled region types must be reversed to their original condition.
    1. AAI_Concrete
    2. AAI_Gravel
- All non-standard filled region types must be removed from the model or replaced with a standard filled region type
    - AAI_Solid_WhiteO
    - Total instance(s): 3
    - Element ID(s):
        - 1353916
        - 1353918
        - 1353920
    - NAAI_Solid_WhiteT
    - Total instance(s): 2
    - Element ID(s):
        - 1353897
        - 1353899
    
─────────────────────────────────────────────
2.2B Filled regions - Non-repeating instances
─────────────────────────────────────────────

// Scenario 1: No filled region present in the documentation views

Model requirements
──────────────────
- Filled region elements should be placed either inside families or turned into detail item elements.

Audit results
─────────────
- Filled regions in documentation views: 0

Action required
───────────────
- None.

// Scenario 2: Either one or more non-repeating filled regions present in the documentation views

Model requirements
──────────────────
- Filled region elements should be placed either inside families or turned into detail item elements.

Audit results
─────────────
- Filled regions in documentation views: 123

Action required
───────────────
- Filled regions should either be placed inside families or converted into detail item elements.
    - View type and name: Floor Plan - CopyMonitor_DATUM
        - Element ID: 1354022 | Filled region type name: AAI_Concrete
        - Element ID: 1354024 | Filled region type name: AAI_Gravel

─────────────────────────────────────────
2.2C Filled regions - Repeating instances
─────────────────────────────────────────

Model requirements
──────────────────
- Repeating filled regions must be replaced with detail item elements.

Audit results
─────────────
- Filled regions in documentation views: 0
- Repeating filled regions in documentation views: N/A

Action required
───────────────
- None.

// Scenario 2: No repeating filled regions present in the documentation views

Model requirements
──────────────────
- Repeating filled regions must be replaced with detail item elements.

Audit results
─────────────
- Filled regions in documentation views: 123
- Repeating filled regions in documentation views: 0

Action required
───────────────
- None.

// Scenario 3: Repeating filled regions present in the documentation views

Model requirements
──────────────────
- Repeating filled regions must be replaced with detail item elements.

Audit results
─────────────
- Filled regions in documentation views: 123
- Repeating filled regions in documentation views: 4

Action required
───────────────
- These repeating filled regions must be deleted or replaced with detail item elements.
    - View type and name: Floor Plan - CopyMonitor_DATUM // group results by combining view type and name
        - Repeating filled region type name: AAI Brickwork | Total instance(s): 4 | Element ID(s): 
            - 1354022
            - 1354031
            - 1354040
            - 1354049

─────────────────
2.3 Line patterns
─────────────────

// Scenario 1: Neither model lines or detail lines visible in documentation views

Model requirements
──────────────────
- RVT models should use only the standard line pattern types.
- Standard line pattern types must not be modified.

Audit results
─────────────
- Model lines in documentation views: 0
- Detail lines in documentation views: 0
- Non-standard line patterns: N/A

Action required
───────────────
- None.

// Scenario 2: Neither model lines or detail lines visible in documentation views with non-standard line patterns

Model requirements
──────────────────
- RVT models should use only the standard line pattern types.
- Standard line pattern types must not be modified.

Audit results
─────────────
- Model lines in documentation views: 20
- Detail lines in documentation views: 3
- Non-standard line patterns: 0

Action required
───────────────
- None.

// Scenario 3: Either model lines in the model or detail lines in documentation views with non-standard line patterns

Model requirements
──────────────────
- RVT models should use only the standard line pattern types.
- Standard line pattern types must not be modified.

Audit results
─────────────
- Model lines in documentation views: 20
- Detail lines in documentation views: 3
- Non-standard line patterns: 2

Action required
───────────────
- Model lines with non-standard line pattern types must be either deleted or changed to use a standard line pattern type.
    - Line pattern name: A-EF_XX_XX-LP_Dash-AAI-DashSpace-X2-00 | Total instance(s): 2 | Element ID(s):
        - 1354022
        - 1354030
    - Line pattern name: A-EF_XX_XX-LP_GridLines-AAI-Center-X2-00 | Total instance(s): 2 | Element ID(s):
        - 1354022
        - 1354030
- Detail lines with non-standard line pattern types must be either deleted or changed to use a standard line pattern type.
    - View type and name: Floor Plan - CopyMonitor_DATUM // group results by combining view type and name
        - Non-standard line pattern name: A-EF_XX_XX-LP_Dash-AAI-DashSpace-X2-00 | Total instance(s): 2 | Element ID(s):
            - 1354022
            - 1354031
        - Non-standard line pattern name: A-EF_XX_XX-LP_GridLines-AAI-Center-X2-00 | Total instance(s): 2 | Element ID(s):
            - 1354040
            - 1354049

─────────────
2.5 Materials
─────────────

// Scenario 1: No modified standard materials in the model

Model requirements
──────────────────
- RVT models should use only the standard materials types.
- Standard materials types must not be modified.

Audit results
─────────────
- Modified standard materials in the model: 0
- Non-standard materials in the model: 0

Action required
───────────────
- None.

// Scenario 2: Either one or more than one modified materials in the model

Model requirements
──────────────────
- RVT models should use only the standard materials types.
- Standard materials types must not be modified.

Audit results
─────────────
- Modified standard materials in the model: 2
- Non-standard materials in the model: 2

Action required
───────────────
- Modified standard materials must be reversed to the standard settings
    - AAI_Concrete
    - AAI_Gravel
- Non-standard materials must be must be either removed or replaced with a standard material.
    - A-XX-M-SST-000
    - bimstore_Formica_HighPressureLaminate

─────────────────────────────────────────────────
2.6A Naming Convention - Host elements type names
─────────────────────────────────────────────────

// Scenario 1: No host elements in the model

Model requirements
──────────────────
- Host element (ceilings, floors, ramps, roofs, stairs, and walls) types names must follow the principles outlined in documents AAIUK-AAI-BM-XX-SD-A-00102 and BIM-AAI-XX-XX-RP-AR-00011

Audit results
─────────────
- Host elements types in the model: 0
- Non-standard host element type names in the model: N/A

Action required
───────────────
- None

// Scenario 2: All host elements type names comply with AAI standards

Model requirements
──────────────────
- Host element (ceilings, floors, ramps, roofs, stairs, and walls) types names must follow the principles outlined in documents AAIUK-AAI-BM-XX-SD-A-00102 and BIM-AAI-XX-XX-RP-AR-00011

Audit results
─────────────
- Host elements types in the model: 123
- Non-standard host element type names in the model: 0

Action required
───────────────
- None

// Scenario 3: At least one host element type name does not comply with with AAI standards

Model requirements
──────────────────
- Host element (ceilings, floors, ramps, roofs, stairs, and walls) types names must follow the principles outlined in AAIUK-AAI-BM-XX-SD-A-00102 and BIM-AAI-XX-XX-RP-AR-00011

Audit results
─────────────
- Host elements types in the model: 123
- Non-standard host element type names in the model: 10

Action required
───────────────
- These host element types must be either removed from the model or renamed to follow the principles outlined in AAIUK-AAI-BM-XX-SD-A-00102 and BIM-AAI-XX-XX-RP-AR-00011.
    - Ceilings // Regex: /AAI_CLG_CAS-[0-9][0-9][0-9](_.*|$)
        - AAI_CLG_Insulation160
        - AAI_Soffit_Placeholder
    - Floors // Regex: /AAI_FLR_(FAS|FFA|FSA)-[0-9][0-9][0-9](_.*|$)
        - AAI_FLR_RaisedAcces114_FAS-506
        - AAI_FLR_PolishedConcrete150_FAS-501
    - Roofs // Regex: /AAI_ROF_(RAS|RFA|RSA)-[0-9][0-9][0-9](_.*|$)
        - AAI_FLR_RaisedAcces114_FAS-506
        - AAI_FLR_PolishedConcrete150_FAS-501
    - Stairs // Regex: /AAI_STA_VST-[0][0-9][0-9](_.*|$)/gm
        - AAI_STA_InsituConcrete_170
        - AAI_STA_InsituConcrete_190
    - Walls // /AAI_WAL_(WAS|WFA)-[0-9][0-9][0-9](_.*|$)/gm
        - AAI_WAL_WAS-303A
        - AAI_WAL_RiserSeparationBeam220

────────────────────────────────────────────────────
2.6B Naming Convention - Other Elements Family Names
────────────────────────────────────────────────────
// Use regex "AAI_ANO_.*", "AAI_BAL_.*", "AAI_CSW_.*", ... for each category
// Not sure if we want to have this level of scrutiny as the naming of types

// Scenario 1: No elements in the model

Model requirements
──────────────────
- Component elements types names must follow the principles outlined in AAIUK-AAI-BM-XX-SD-A-00102.

Audit results
─────────────
- Component element families in the model: 0
- Non-standard element family names in the model: N/A

Action required
───────────────
- None.

// Scenario 2: All elements family names comply with AAI standards

Model requirements
──────────────────
- Component elements types names must follow the principles outlined in AAIUK-AAI-BM-XX-SD-A-00102.

Audit results
─────────────
- Component element families in the model: 25
- Non-standard element family names in the model: 0

Action required
───────────────
- None.

// Scenario 3: At least one family names does not comply with AAI standards

Model requirements
──────────────────
- Component elements types names must follow the principles outlined in AAIUK-AAI-BM-XX-SD-A-00102.

Audit results
─────────────
- Component element families in the model: 25
- Non-standard element family names in the model: 3

Action required
───────────────
- These component element types must be removed from the model or renamed to match the criteria from AAIUK-AAI-BM-XX-SD-A-00102
    - Doors
        - AAI_GlassDoorPanel
    - Furniture
        - AAI_CHG_Bench
    - Plumbing fixtures
        - DC34

───────────────────────────────────────
2.6C Naming Convention - View Templates
───────────────────────────────────────

// Use regex "AAI_([0-9][0-9]|[XZ][XZ]).*"
// Scenario 1: No view templates in the model

Model requirements
──────────────────
- View template names must follow the principles outlined in AAIUK-AAI-BM-XX-SD-A-00102.

Audit results
─────────────
- View templates in the model: 0
- Non-standard view template names in the model: N/A

Action required
───────────────
- None.

// Scenario 2: All view template names comply with AAI standards

Model requirements
──────────────────
- View template names must follow the principles outlined in AAIUK-AAI-BM-XX-SD-A-00102.

Audit results
─────────────
- View templates in the model: 20
- Non-standard view template names in the model: 0

Action required
───────────────
- None.

// Scenario 3: At least one view template name does not comply with with AAI standards

Model requirements
──────────────────
- View template names must follow the principles outlined in AAIUK-AAI-BM-XX-SD-A-00102.

Audit results
─────────────
- View templates in the model: 20
- Non-standard view template names in the model: 2

Action required
───────────────
- These view templates must be removed from the model or renamed them to match the criteria from AAIUK-AAI-BM-XX-SD-A-00102
    - AAI_FRB_DesignOptions3D
    - FRB_Demolition

────────────────────────────────
2.6D Naming Convention - Filters
────────────────────────────────

// Use regex "AAI_([0-9][0-9]|[XZ][XZ]).*"
// Scenario 1: No filters in the model

Model requirements
──────────────────
- Filter names must follow the principles outlined in AAIUK-AAI-BM-XX-SD-A-00102.

Audit results
─────────────
- Filters in the model: 0
- Non-standard filter names in the model: N/A

Action required
───────────────
- None.

// Scenario 2: All filter names comply with AAI standards

Model requirements
──────────────────
- Filter names must follow the principles outlined in AAIUK-AAI-BM-XX-SD-A-00102.

Audit results
─────────────
- Filters in the model: 20
- Non-standard filter names in the model: 0

Action required
───────────────
- None.

// Scenario 3: At least one view template name does not comply with with AAI standards

Model requirements
──────────────────
- Filter names must follow the principles outlined in AAIUK-AAI-BM-XX-SD-A-00102.

Audit results
─────────────
- Filters in the model: 20
- Non-standard filter names in the model: 2

Action required
───────────────
- These filters must be removed from the model or renamed to match the criteria from AAIUK-AAI-BM-XX-SD-A-00102
    - AAI_FRB_DesignOptions3D
    - FRB_Demolition

───────────────────────────────────────
2.6E Naming Convention - Filled regions
───────────────────────────────────────

// Use regex  @"AAI_[a-zA-Z]+_[a-zA-Z0-9_]+"gm

// Scenario 1: No filled regions in the model

Model requirements
──────────────────
- Filled region names must follow the principles outlined in AAIUK-AAI-BM-XX-SD-A-00102.

Audit results
─────────────
- Filled region types in the model: 0
- Non-standard filled region type names in the model: N/A

Action required
───────────────
- None.

// Scenario 2: All filled regions in the model follow the naming principles

Model requirements
──────────────────
- Filled region names must follow the principles outlined in AAIUK-AAI-BM-XX-SD-A-00102.

Audit results
─────────────
- Filled region types in the model: 20
- Non-standard filled region type names in the model: 0

Action required
───────────────
- None.

// Scenario 3: At least one filled region in the model does not follow the naming principles

Model requirements
──────────────────
- Filled region names must follow the principles outlined in AAIUK-AAI-BM-XX-SD-A-00102.

Audit results
─────────────
- Filled region types in the model: 20
- Non-standard filled region type names in the model: 2

Action required
───────────────
- These filled region types must be either removed from the model or renamed to follow AAIUK-AAI-BM-XX-SD-A-00102.
    - Glass
    - AAI_Red

──────────────────────────────────────
2.6F Naming Convention - Fill patterns
──────────────────────────────────────

// Scenario 1: No fill patterns in the model (higly unlikely)
- Fill patterns in the model: 0
- Non-standard fill pattern names in the model: N/A
- Action required: None.

// Scenario 2: All fill patterns names in the model comply with AAI standards
- Fill patterns in the model: 30
- Non-standard fill pattern names in the model: 0
- Action required: None.

// Scenario 3: At least one fill pattern name in the model does not comply with AAI standards
- Fill patterns in the model: 30
- Non-standard fill pattern names in the model: 4
- Action required: Remove the following fill patterns from the 

────────────────────────────────
2.6G Naming Convention - Line Styles
────────────────────────────────
// Combine with analysis of line styles?

────────────────────────────────
2.6H Naming Convention - Line Patterns
────────────────────────────────
// Combine with analysis of line styles?

──────────────────
2.7A Object Styles
──────────────────

// Scenario 1: All object styles in the model comply with AAI standards
- Non-standard object styles in the model: 0
- Action required: None.

// Scenario 2: At least one object style in the model does not comply with AAI standards
- Non-standard object styles in the model: 3
- Action required: These object styles must be reversed to their original condition
    - Model Objects: Casework - <Hidden Lines>
        - Line Color current value: RGB 255-000-128 - Change to: Black
        - Line Pattern current value: AAI_Dash_1.5 - Change to: AAI_Dash_3
    - Annotation Objects: Door Tags
        - Line Weight current value: 4 - Change to: 2

────────────────────────────
2.8A Project Browser - Views
────────────────────────────

// Values in views in AAI_Series that don't match "/(.MANAGEMENT)|(WIP(_[A-Z]{3}))|([0-9]0{4} .*)/gm"
// Scenario 1: All AAI_Series values of views in the model comply with AAI standards
- Non-standard AAI_Series values in views in the model: 0
- Action required: None.

// Scenario 2: At least one AAI_Series values of views in the model does not comply with AAI standards
- Non-standard AAI_Series values views in the model: 4
- Action Required: Remove the views with the AAI_Series values listed below or assign them a standard value
    - AAI_Series value: 2XXXX-GENERAL ARRANGEMENT DRAWINGS
        - View Name: GENERAL ARRANGEMENT PLAN GROUND LEVEL Copy 4 | Element ID: 123456
        - View Name: Level 20(1) Copy 1 | Element ID: 123457
    - AAI_Series value: AAI_SKETCHES
        - View Name: Existing Part Wall Plan - Ground Level - AAI Intent | Element ID: 123458
        - View Name: PLAN LEVEL 10 - WALL TYPE STUDY | Element ID: 123459
- Standard AAI_Series values
    - 00000 PROJECT GENERIC INFORMATION AND SCHEDULES
    - 10000 SITE INFORMATION AND OVERALL DRAWINGS
    - 20000 GENERAL ARRANGEMENT DRAWINGS
    - 30000 CORE/ENLARGED DRAWINGS
    - 40000 REFLECTED CEILING DRAWINGS AND CEILING DETAILS
    - 50000 BUILDING/CORE ELEVATIONS AND SECTIONS
    - 60000 VERTICAL CIRCULATION
    - 70000 BUILDING ENVELOPE AND EXTERIOR HARD LANDSCAPING
    - 80000 INTERIORS DETAILED INFORMATION
    - 90000 STATUTORY SIGNAGE AND OTHER PROJECT SPECIFIC PACKAGES
    - WIP_XXX where XXX is the three letter code of each AAI team member working on the project

────────────────────────────────
2.8B Project Browser - Schedules
────────────────────────────────

// Values in schedules in AAI_Series that don't match "/(.MANAGEMENT)|(WIP(_[A-Z]{3}|))|(0{5} .*)/gm"
// Scenario 1: All AAI_Series values of schedules in the model comply with AAI standards
- Non-standard AAI_Series values in schedules in the model: 0
- Action required: None.

// Scenario 2: At least one AAI_Series values of schedules in the model does not comply with AAI standards
- Non-standard AAI_Series values in schedules in the model: 4
- Action Required: Remove the schedules with the AAI_Series values listed below or assign them a standard value
    - AAI_Series value: AAI_Working
        - Schedule Name: LCB Room Schedule | Element ID: 123456
    - AAI_Series value: empty (???)
        - Schedule Name: Door Schedule 5 | Element ID: 123458
        - Schedule Name: Specialty Equipment Schedule | Element ID: 123459
- Standard AAI_Series values for schedules
    - 00000 PROJECT GENERIC INFORMATION AND SCHEDULES
    - WIP_XXX where XXX is the three letter code of each AAI team member working on the project

─────────────────────────────
2.8C Project Browser - Sheets
─────────────────────────────

// Values in sheets in AAI_Series that don't match "/(.MANAGEMENT)|(WITHDRAWN \/ SUPERSEDED \/ DELETED)|([0-9]0{4} .*)/gm"
// Scenario 1: All AAI_Series values of sheets in the model comply with AAI standards
- Non-standard AAI_Series values in sheets in the model: 0
- Action required: None.

// Scenario 2: At least one AAI_Series values of schedules in the model does not comply with AAI standards
- Non-standard AAI_Series values in sheets in the model: 4
- Action Required: Remove the sheets with the AAI_Series values listed below or assign them a standard value
    - AAI_Series value: empty (???)
        - Sheet Number: S0193 | Element ID: 123456
        - Sheet Number: S0245.6 | Element ID: 123456
    - AAI_Series value: NOT IN USE
        - Sheet Number: 52021X | Element ID: 123456
        - Sheet Number: SS-60001 | Element ID: 123456
- Standard AAI_Series values for sheets
    - 00000 PROJECT GENERIC INFORMATION AND SCHEDULES
    - 10000 SITE INFORMATION AND OVERALL DRAWINGS
    - 20000 GENERAL ARRANGEMENT DRAWINGS
    - 30000 CORE/ENLARGED DRAWINGS
    - 40000 REFLECTED CEILING DRAWINGS AND CEILING DETAILS
    - 50000 BUILDING/CORE ELEVATIONS AND SECTIONS
    - 60000 VERTICAL CIRCULATION
    - 70000 BUILDING ENVELOPE AND EXTERIOR HARD LANDSCAPING
    - 80000 INTERIORS DETAILED INFORMATION
    - 90000 STATUTORY SIGNAGE AND OTHER PROJECT SPECIFIC PACKAGES
    - WITHDRAWN / SUPERSEDED / DELETED

───────────────
2.9 Revit Links
───────────────

// Scenario 1: No RVT links in the model

- RVT link types in the model: 0
- Action required: None.

// Scenario 2: RVT links in the model, single instances

- RVT link types in the model: 3
    - EST-AAI-ZZ-ZZ-M3-A-00001.rvt
    - EST-AAI-ZZ-ZZ-M3-A-00002.rvt
    - EST-AAI-ZZ-ZZ-M3-A-00003.rvt
    - Redundant instances of RVT links: 0
- Action required: None.

// Scenario 3: RVT links in the model, multiple instances instances

- Linked DWGs types in the model: 3
    - EST-AAI-ZZ-ZZ-M3-A-00001.rvt (multiple instances)
    - EST-AAI-ZZ-ZZ-M3-A-00002.rvt
    - EST-AAI-ZZ-ZZ-M3-A-00003.rvt (multiple instances)
    - Redundant instances of linked DWGs: 5

- Action required: Delete elements as necessary to avoid redundant instances.
    - Revit link name: EST-AAI-ZZ-ZZ-M3-A-00001.rvt
        - Instance(s) to delete: 1
        - Element ID(s): 
            - 1242178
            - 1242180

    - Revit link name: EST-AAI-ZZ-ZZ-M3-A-00003.rvt
        - Instance(s) to delete: 2
        - Element ID(s):
            - 1243692
            - 1243693
            - 1243696

──────────────
2.10 Revisions
──────────────

// Scenario 1: No revision sequences in the model

- Revision sequences in the model: 0
- Action required: None.

// Scenario 2: No unused revision sequences in the model

- Revision sequences in the model: 3
    - Sequence: 1 | Numbering: AAI_Preliminary | Date: 07 JUN 2024 | Description: PHASE 1 - STAGE 3
    - Sequence: 2 | Numbering: AAI_Preliminary | Date: 28 JUN 2024 | Description: FOR INFORMATION
    - Sequence: 3 | Numbering: AAI_Preliminary | Date: 16 JUL 2024 | Description: FOR INFORMATION
- Action required: None.

// Scenario 3: Unused revision sequences in the model

- Revision sequences in the model: 4
    - Sequence: 1 | Numbering: AAI_Preliminary | Date: 07 JUN 2024 | Description: PHASE 1 - STAGE 3
    - Sequence: 2 | Numbering: AAI_Preliminary | Date: 07 JUN 2024 | Description: FOR INFORMATION | (NOT IN USE)
    - Sequence: 3 | Numbering: AAI_Preliminary | Date: 28 JUN 2024 | Description: FOR INFORMATION
    - Sequence: 4 | Numbering: AAI_Preliminary | Date: 16 JUL 2024 | Description: FOR INFORMATION
- Action required: Delete the following revision sequences or associate them to a documentation sheet
    - Sequence: 2

──────────────────────────────
2.11A Worksets - Linked models
──────────────────────────────

// Scenario 1: No RVT links in the model

- RVT link types in the model: 0
- Action required: None.

// Scenario 2: RVT links in the model, each link is in a dedicated model AND each workset is correctly named

- RVT link types in the model: 3
    - Link name: TEH-TLA-ZZ-ZZ-M3-L-00001 | Current workset name: LinkRVT_L_TEH-TLA-ZZ-ZZ-M3-L-00001
    - Link name: TEH-WSP-ZZ-ZZ-M3-Z-00001 | Current workset name: LinkRVT_M_TEH-WSP-ZZ-ZZ-M3-Z-00001
    - Link name: TEH-AKT-A0-ZZ-M3-Z-00013 | Current workset name: LinkRVT_S_TEH-AKT-A0-ZZ-M3-Z-00013
- Action required: None.

// Scenario 3: RVT links in the model, either more than one link is in the same workset OR the worksets are not correctly named
- RVT link types in the model: 3
    - Link name: TEH-TLA-ZZ-ZZ-M3-L-00001 | Current workset name: LinkRVT_L_TEH-TLA-ZZ-ZZ-M3-L-00001
    - Link name: TEH-WSP-ZZ-ZZ-M3-Z-00001 | Current workset name: LinkRVT_L_TEH-TLA-ZZ-ZZ-M3-L-00001
    - Link name: TEH-AKT-A0-ZZ-M3-Z-00013 | Current workset name: AAI_ScopeBoxes
- Action required: Move each of the following links to a dedicated workset
    - Link name: TEH-WSP-ZZ-ZZ-M3-Z-00001 | Dedicated workset name: LinkRVT_M_TEH-WSP-ZZ-ZZ-M3-Z-00001
    - Link name: TEH-AKT-A0-ZZ-M3-Z-00013 | Dedicated workset name: LinkRVT_S_TEH-AKT-A0-ZZ-M3-Z-00013

──────────────────────────────
2.11B Worksets - 3D elements
──────────────────────────────

// Scenario 1: No 3D elements in the model
- 3D Elements in the model: 0
- Action required: None.

// Scenario 2: 3D elements in the model
- 3D Elements in the model: 312
- Worksets in the model containing 3D Elements: 
    - AAI_A0_General
    - AAI_A0_SpecialtyEquipment
- Action required: None.

// Scenario 3: 3D elements in the model placed in the wrong workset
- 3D Elements in the model: 312
    - Workset name: AAI_A0_General
    - Workset name: LinkRVT_S_TEH-AKT-A0-ZZ-M3-Z-00013
- Action required: Move the following elements to a workset not dedicated to linked models (workset's name to begin with "AAI_")
    - Element ID: 1243692 | Current workset name: LinkRVT_S_TEH-AKT-A0-ZZ-M3-Z-00013
    - Element ID: 1243693 | Current workset name: LinkRVT_S_TEH-AKT-A0-ZZ-M3-Z-00013
    - Element ID: 1243696 | Current workset name: LinkRVT_S_TEH-AKT-A0-ZZ-M3-Z-00013
- Available worksets in the model
    - AAI_A0_General
    - AAI_A0_SpecialtyEquipment
```
