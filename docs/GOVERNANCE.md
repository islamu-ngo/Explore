# Code Conventions

## C# Style Guide

- **Naming**: PascalCase for public members, _camelCase for private fields
- **File-scoped namespaces**: Use file-scoped namespace declarations

## CQRS Pattern


## Repository Pattern

## Mapping

If DTO property is named EventTitle and your Entity path is Event.Title, AutoMapper automatically figures this out. You don't need .ForMember.
CreateMap<EventSession, EventSessionDto>()
    // You only need manual mapping if names DO NOT match
    .ReverseMap();

## Validation
Location: DTOs/{Entity}/Validators/


## AutoMapper Profiles
