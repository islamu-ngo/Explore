# Plan: UI Refactor - Luma-Inspired Redesign

## Executive Summary
This plan outlines a comprehensive UI overhaul of the ISLAMU Event platform to align with a modern, Luma-inspired aesthetic. The primary goals are to enhance visual hierarchy, improve readability with optimized typography, introduce a distinct "Luma Green" brand identity, and resolve contrast issues in light mode where card separation is unclear.

The redesign will focus on:
1.  **Visual Identity**: Implementing a high-contrast theme with Luma-style green accents and modern typography.
2.  **Card Clarity**: Redesigning event cards to be distinctly separated from the background in both light and dark modes.
3.  **Detail Experience**: Refactoring the Event Detail page to feature a cinematic hero section, sticky registration sidebar, and clean content layout.

## Current State Analysis
-   **Theme**: Uses default MudBlazor typography. Colors are generic (Primary blue/purple).
-   **Light Mode**: Lack of separation between cards and background (likely 0 elevation or same color).
-   **Event List**: Standard grid, functional but visually flat.
-   **Event Detail**: Functional, but lacks visual impact. Standard container width limits "cinematic" feel.

## Proposed Future State
-   **Theme**:
    -   **Primary Color**: Luma Green (`#00D16F` or similar vibrant green).
    -   **Typography**: `Inter` or `Plus Jakarta Sans` for clean, modern readability.
    -   **Dark Mode**: Deep blue/black background (`#0a0a0a` or `#111`), not gray.
    -   **Light Mode**: Off-white background (`#f5f5f7`) with pure white cards (`#ffffff`) and subtle borders/shadows.
-   **Event List**:
    -   Cards with distinct borders/shadows.
    -   Hover effects for interactivity.
    -   Cleaner filter interface.
-   **Event Detail**:
    -   **Hero**: Full-width or large container image with gradient overlay.
    -   **Title**: Large, bold typography (H1/Display).
    -   **Sidebar**: Sticky registration card with clear call-to-action.
    -   **Content**: Centered, readable width (max-width ~700px for text).

## Implementation Phases

### Phase 1: Foundation (Theme & Typography)
#### Task 1.1: Implement Luma Color Palette
-   **File**: `Explore.Blazor.Client/Layout/MainLayout.razor.cs`
-   **Action**: Update `_lightPalette` and `_darkPalette`.
-   **Details**:
    -   Set `Primary` to Luma Green.
    -   Adjust `Background` vs `Surface` colors for contrast.
    -   Configure `AppbarBackground` to be semi-transparent blur.

#### Task 1.2: Configure Typography
-   **File**: `Explore.Blazor.Client/Layout/MainLayout.razor` & `MainLayout.razor.cs`
-   **Action**: Add Google Fonts link (Inter/Plus Jakarta Sans). Update `MudTheme.Typography`.
-   **Details**:
    -   H1-H6: Bold, tight letter spacing.
    -   Body: Readable line height (1.6).

### Phase 2: Component Refactoring (Event List)
#### Task 2.1: Refactor Event Card Style
-   **File**: `Explore.Blazor.Client/Pages/Event/EventList.razor`
-   **Action**: Update `MudCard` styling.
-   **Details**:
    -   Add `Outlined="true"` or distinct `Elevation`.
    -   Ensure background color is `Surface` (White/DarkGrey) against `Background` (Off-white/Black).
    -   Improve image aspect ratio and typography hierarchy within the card.

#### Task 2.2: Modernize Filter Header
-   **File**: `Explore.Blazor.Client/Pages/Event/EventList.razor`
-   **Action**: Style the sticky header.
-   **Details**:
    -   Use `Variant.Outlined` with rounded corners for inputs.
    -   Ensure distinct background for the sticky bar.

### Phase 3: Component Refactoring (Event Detail)
#### Task 3.1: Hero Section Redesign
-   **File**: `Explore.Blazor.Client/Pages/Event/EventDetail.razor`
-   **Action**: Create a new Hero section.
-   **Details**:
    -   Use `MudImage` or `div` with background image.
    -   Overlay title and organizer info on the image (or right below in a clean header).
    -   Ensure responsive height.

#### Task 3.2: Content & Sidebar Layout
-   **File**: `Explore.Blazor.Client/Pages/Event/EventDetail.razor`
-   **Action**: Refine grid layout.
-   **Details**:
    -   Main content column (Left/Center): Description, Agenda, Aspects.
    -   Sidebar column (Right, Sticky): Registration card.
    -   Ensure sidebar stays visible during scroll.

## Risk Assessment
-   **Contrast**: Ensuring the green primary color is accessible on both light and dark backgrounds.
    -   *Mitigation*: Use a slightly darker shade for text/icons on light backgrounds if needed, or check WCAG contrast.
-   **Responsiveness**: Custom layouts (Hero, Sticky Sidebar) must work on mobile.
    -   *Mitigation*: Use standard MudBlazor breakpoints (`xs`, `md`, `lg`).

## Required Resources
-   Google Fonts (Inter or similar).
-   MudBlazor documentation for Theme customization.

## Effort Estimates
-   Phase 1: Small (1-2 hours)
-   Phase 2: Medium (2-4 hours)
-   Phase 3: Medium (2-4 hours)
