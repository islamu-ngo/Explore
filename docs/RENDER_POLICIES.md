# Render Policies

> **Policy-Based UI Rendering Architecture**
>
> This document describes how the platform manages Blazor render modes dynamically
> based on policies, not hardcoded configurations.

**Last Updated**: January 2026

---

## Overview

The platform uses a **Policy-Based Render Mode System** that separates technical implementation (Server, WebAssembly, Auto) from business decisions (which pages need which level of interactivity). This enables Instance Admins to optimize resource usage without code changes.

---

## The Problem

### Traditional Approach Issues

- Render modes hardcoded per component
- Changing modes requires code deployment
- One-size-fits-all doesn't match varied tenant needs
- Resource optimization is developer-driven, not operations-driven

### Policy-Based Solution

- Pages classified by logical type, not technical mode
- Policies map logical types to render modes
- Instance Admin controls the mapping
- Tenant Admin may override (if permitted)

---

## Page Classification System

### Logical Page Types

Pages are tagged with a logical classification, independent of render technology:

| Type | Characteristics | Examples |
|------|-----------------|----------|
| **Content** | Read-heavy, cacheable, SEO-important | Listings, blogs, public profiles |
| **Operational** | Interactive, form-heavy, real-time updates | Dashboards, editors, admin panels |
| **Transactional** | Critical operations, payment flows | Checkout, registration completion |
| **Static** | Rarely changes, no interactivity | About, legal, help pages |

### Classification Guidelines

| Criteria | Content | Operational |
|----------|---------|-------------|
| User interaction | Low | High |
| Data freshness | Can be stale | Must be current |
| SEO importance | High | Low |
| Server load | Low per view | Higher per session |

---

## Render Mode Options

### Available Modes

| Mode | Description | Best For |
|------|-------------|----------|
| **Static** | Pre-rendered, no interactivity | Content pages, SEO |
| **Server** | SignalR connection, server-side execution | Operational pages, complex logic |
| **WebAssembly** | Client-side execution | Offline capability, reduced server load |
| **Auto** | Server initially, transitions to WASM | Best of both worlds |

### Mode Trade-offs

| Factor | Static | Server | WASM | Auto |
|--------|--------|--------|------|------|
| Initial load | ⚡ Fastest | Fast | Slow | Fast |
| Interactivity | ❌ None | ✅ Full | ✅ Full | ✅ Full |
| Server resources | ⚡ Minimal | ⬆️ Higher | ⚡ Minimal | Medium |
| Offline support | ❌ No | ❌ No | ✅ Yes | ⚡ Partial |
| SEO | ✅ Best | ✅ Good | ⚠️ Challenging | ✅ Good |

---

## Policy Definition

### Resolution Matrix

Instance Admin defines how page types map to render modes:

| Policy Name | Content | Operational | Transactional | Use Case |
|-------------|---------|-------------|---------------|----------|
| **Performance Saver** | Static | WASM | Server | Free tier, low server cost |
| **Premium Fast** | Static | Server | Server | Paid tier, responsiveness priority |
| **Modern Edge** | Auto | Auto | Auto | Modern devices, best UX |
| **Compatibility** | Static | Server | Server | Legacy browser support |

### Policy Assignment

| Level | Can Assign | Constraints |
|-------|------------|-------------|
| System Default | Implicit | Fallback only |
| Instance | Instance Admin | Any combination |
| Tenant | Tenant Admin | Within allowed set |

---

## Implementation Architecture

### Policy Resolution Flow

1. **Page Request** → User navigates to page
2. **Type Lookup** → System identifies page's logical type
3. **Policy Check** → Resolver queries current policy
4. **Mode Selection** → Policy maps type to render mode
5. **Rendering** → Page renders with selected mode

### Policy Service Interface

The policy service answers: "Given this page type and context, what render mode should be used?"

**Inputs**:
- Page type (Content, Operational, etc.)
- Tenant context (if any)
- Device hints (optional)
- User preferences (optional)

**Output**:
- Render mode to apply

### Dynamic Wrapper Component

A wrapper component at the layout level:
1. Reads the policy for the current page type
2. Applies the resolved render mode
3. Renders the actual page content

---

## Configuration Model

### Instance-Level Settings

| Setting | Values | Default |
|---------|--------|---------|
| Default Policy | Policy name | "Modern Edge" |
| Allow Tenant Override | Boolean | true |
| Allowed Tenant Policies | Policy list | All |

### Tenant-Level Settings

| Setting | Values | Constraint |
|---------|--------|------------|
| Active Policy | Policy name | Must be in allowed set |
| Custom Mappings | Type → Mode | If customization enabled |

---

## Optimization Strategies

### Resource-Based Policies

| Tenant Tier | Recommended Policy | Rationale |
|-------------|-------------------|-----------|
| Free | Performance Saver | Minimize server usage |
| Standard | Modern Edge | Balanced experience |
| Premium | Premium Fast | Best responsiveness |
| Enterprise | Custom | Tailored to needs |

### Time-Based Policies

Policies can vary by time of day or load conditions:

| Condition | Policy | Rationale |
|-----------|--------|-----------|
| Peak hours | Performance Saver | Reduce server strain |
| Off-peak | Premium Fast | Better UX when capacity available |
| High load | Performance Saver | Graceful degradation |

---

## Migration Path

### From Hardcoded to Policy-Based

1. **Audit** - Document current render mode per component
2. **Classify** - Assign logical type to each page
3. **Create Baseline** - Define policy matching current behavior
4. **Implement Wrapper** - Add policy resolution to layout
5. **Test** - Verify equivalent behavior
6. **Enable Policies** - Expose policy selection to admins
7. **Optimize** - Create additional policies for different scenarios

---

## Related Documentation

- **[BLAZOR.md](BLAZOR.md)** - Blazor architecture overview
- **[MULTI_TENANCY.md](MULTI_TENANCY.md)** - Tenant configuration
- **[ADMIN_HIERARCHY.md](ADMIN_HIERARCHY.md)** - Policy authority

## Implementation Reference

For code patterns:
- **`blazor-bff-patterns`** skill - BFF and rendering
- **`blazor-ui-conventions`** skill - Component patterns
