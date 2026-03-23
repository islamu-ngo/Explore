# Lessons Learned

## 2026-03-21: Do NOT add IsUnlisted boolean — use VisibilityTypeEnum

**Mistake:** Added `IsUnlisted` boolean property to Event entity when `VisibilityTypeEnum.Unlisted = 3` already exists as a lookup table value.

**Root cause:** Did not check existing lookup tables (VisibilityType has Public=1, Private=2, Unlisted=3, MembersOnly=4) before adding a new domain property.

**Rule:** Before adding a boolean flag to a domain entity, always check if an existing lookup/enum table already covers the concept. In this project, visibility is modeled via `VisibilityTypeId` FK to `VisibilityType` lookup table with `VisibilityTypeEnum`.
