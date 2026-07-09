ABOUTME: Technical report on localization architecture, TMS abstraction (Tolgee/Weblate), offline bundles, and .NET integration.
ABOUTME: Guides self-hosters on configuring providers and outlines robust static bundle fallback and update strategies.

# ISLAMU Event Internationalization & Translation Report

## 1. Executive Summary

This report provides an in-depth technical and architectural evaluation of the localization infrastructure of the ISLAMU Event platform. The objective is to design a robust, scalable, and secure multilingual system that functions in distributed, cloud-native environments and simple, offline-first scenarios for self-hosters.

This document evaluates the trade-offs between traditional .NET `.resx`-based localization and a modern API/JSON-driven approach. It analyzes integration strategies for Translation Management Systems (TMS) such as Tolgee and Weblate, runtime constraints in Blazor WebAssembly (WASM), security risks (e.g., XML External Entity (XXE) vulnerabilities), and operational challenges (e.g., network failure fallbacks and translation drift during container updates). Finally, it outlines a concrete database lookup translation strategy.

---

## 2. Translation Formats in .NET: RESX vs. Dynamic JSON

The choice of translation format determines runtime flexibility, CI/CD pipeline complexity, and the ease with which external contributors can translate the application. While .NET natively supports XML-based `.resx` files, modern web applications leverage dynamic JSON files to optimize runtime load times and minimize parser overhead.

The table below compares the .NET `.resx` architecture with the proposed dynamic JSON architecture:

| Feature | .NET RESX Architecture | Dynamic JSON Architecture |
| :--- | :--- | :--- |
| **File Format** | Monolingual XML files with metadata headers. | Compact key-value pairs (flat or nested JSON). |
| **Type-Safety** | Strongly-typed code generation (`Designer.cs`) via compiler. | String-based runtime lookups (`ITranslationResolver` / `ITranslationService`). |
| **Loading Mechanism** | Binary compilation into satellite assemblies (`.resources.dll`). | Dynamic asynchronous HTTP fetches or embedded resources. |
| **Pluralization** | No native support within Weblate's XML parser. | Native CLDR / i18next pluralization syntax. |
| **Security Profile** | Vulnerable to XXE attacks via insecure XML parsers. | Inherently immune to XML-specific parser injections. |
| **Tolgee Support** | Push/pull support since early 2025. | Native support with automatic JSON flattening. |
| **Weblate Support** | Supported via component configuration. | Native export via the `/file/` REST API. |

### 2.1. Tolgee RESX Integration & Security Risks (CVE-2026-32251)
Although Tolgee introduced official `.resx` support in early 2025 (converting curly-brace placeholders like `{0}` to universal ICU format), processing XML-based formats introduces security risks. 

In early 2026, a critical vulnerability was disclosed as **CVE-2026-32251** (CVSS score **9.3**). Tolgee's XML parsers (specifically the `ResxProcessor` for .NET resource files) were vulnerable to XML External Entity (XXE) injection. An authenticated attacker could upload a malicious `.resx` file containing a Document Type Definition (DTD) pointing to local system paths:

```xml
<?xml version="1.0" encoding="utf-8"?>
<!DOCTYPE root [
  <!ENTITY xxe SYSTEM "file:///etc/passwd">
]>
<root>
  <data name="AttackKey" xml:space="preserve">
    <value>&xxe;</value>
  </data>
</root>
```

When parsing the file, the server resolved the external entity `&xxe;`, writing sensitive files (like `/etc/passwd` or container environment variables via `/proc/self/environ`) directly into the translation database. While patched in Tolgee `v3.166.3` by disabling DTD/external entity resolution on the `XMLInputFactory`, this highlights the security advantage of using text-based JSON, which is immune to XML parser vectors.

### 2.2. Weblate RESX Integration & Limitations
Weblate supports `.resx` files as monolingual XML components, configured using a file mask and monolingual base file:

* **File mask**: `Resources/Language.*.resx`
* **Base file**: `Resources/Language.resx`
* **Format ID**: `resx`

While Weblate reads XML metadata and `<comment>` elements as context descriptions for translators, it lacks support for pluralization, context attributes, or location-markers in `.resx` components. Furthermore, Weblate requires creating a separate component for each `.resx` file. In large projects with hundreds of `.csproj` and resource files, this becomes difficult to manage without automating component discovery via the Weblate Python CLI (`wlc`).

---

## 3. Dynamic Runtime Localization in Blazor WebAssembly

A critical requirement for client-side web applications is culture switching without reloading the browser page. This behavior is heavily constrained by how Blazor WebAssembly (WASM) loads localized strings.

### 3.1. The Satellite Assemblies Problem
When using `.resx` files, the .NET SDK compiles localized resources into binary satellite assemblies (e.g., `/fr-FR/App.resources.dll`). At startup, the browser detects the client culture and downloads only the satellite assembly matching the active culture.

If a user switches language inside the app, the new strings are unavailable because the alternative satellite assembly is not loaded in the WASM sandbox. The default .NET behavior forces a browser page-reload (`NavigationManager.NavigateTo(..., forceLoad: true)`) to fetch the alternative DLL during boot. This breaks the user experience and clears in-memory state (e.g., half-filled forms).

### 3.2. Technical Workarounds for Satellite Assemblies
To avoid page reloads with satellite assemblies, developers must use complex workarounds:

* **Eager Loading via WebAssembly Configuration**: Force the client to download all satellite assemblies at initial startup. This requires disabling Blazor's automatic boot in the host page:
  ```html
  <script src="_framework/blazor.web.js" autostart="false"></script>
  ```
  And manually starting the runtime via JavaScript to load satellite resources eagerly:
  ```javascript
  <script>
    Blazor.start({
      webAssembly: {
        configureRuntime: dotnet => {
          dotnet.withConfig({
            loadAllSatelliteResources: true
          });
        }
      }
    });
  </script>
  ```
  *Drawback*: This inflates the initial payload, impacting users on low-bandwidth networks.

* **Internal WebAssembly API Invocations**: Dynamically load satellite assemblies at runtime by calling the undocumented .NET method `INTERNAL.loadSatelliteAssemblies` via JS Interop (used by libraries like `Blazor.WebAssembly.DynamicCulture`).
  *Drawback*: Using undocumented APIs is fragile and prone to breaking during minor .NET updates.

### 3.3. The Dynamic JSON Solution
By coupling a custom `ITranslationService` to flat JSON files loaded asynchronously, these runtime limitations are avoided. Flat JSON files are retrieved via standard HTTP calls as text:
1. When a culture changes, the client requests the target language JSON bundle (lazy loading).
2. The response is cached in memory.
3. The translation service triggers an `OnLanguageChanged` event.
4. Active UI components refresh themselves by calling Blazor's native `StateHasChanged()`.

This method avoids browser reloads, uses only public APIs, and limits initial downloads to the requested culture.

#### Memory Footprint Model
For eager-loaded satellite assemblies, the client memory overhead $M_{\text{eager}}$ scales with the total number of supported cultures $C$:
$$M_{\text{eager}} = \sum_{i \in C} \text{Size}(A_i)$$

For lazy-loaded JSON bundles, the memory overhead $M_{\text{lazy}}$ is limited to the active culture $c_{\text{active}}$:
$$M_{\text{lazy}} = \text{Size}(\text{JSON}_{c_{\text{active}}})$$

This demonstrates the performance and scalability of the dynamic JSON architecture in resource-constrained client environments.

---

## 4. Cloud-Native & Docker Stateless Container Constraints

For production deployments of ISLAMU Event, a stateless container architecture (e.g., Kubernetes, AWS ECS, or replicas behind a load balancer) is required. This introduces constraints on local disk writes.

### 4.1. The Replica Inconsistency Problem
When an administrator triggers "Export from TMS" in the dashboard, the application queries Weblate or Tolgee and persists the resulting JSON file locally to `{ContentRoot}/App_Data/Localization/Bundles/{lang}.json`. In a distributed environment, this creates an inconsistency:

```
                      [ Load Balancer ]
                             │
         ┌───────────────────┼───────────────────┐
         ▼                   ▼                   ▼
    ┌───────────┐       ┌───────────┐       ┌───────────┐
    │ Replica A │       │ Replica B │       │ Replica C │
    └─────┬─────┘       └─────┬─────┘       └─────┬─────┘
          │ (Writes)          │                   │
          ▼                   ▼                   ▼
     [Local JSON]       [Stale JSON]        [Stale JSON]
```

Only the replica that processed the export request (Replica A) updates its local storage. Replicas B and C remain stale. Users routed to B or C will see old translations.

### 4.2. The IBundleFileWriter Seam
To resolve this, the writing mechanism is decoupled via the `IBundleFileWriter` interface:
```csharp
public interface IBundleFileWriter
{
    Task<string> WriteBundleAsync(string languageCode, IReadOnlyDictionary<string, string> translations, CancellationToken ct = default);
    Task<WritablePathHealth> CheckHealthAsync(CancellationToken ct = default);
}
```
* **`BundleFileWriter` (Local)**: Writes to the local container disk. Ideal for single-instance Docker environments used by small self-hosters.
* **`DistributedBundleFileWriter` (Production)**: Overrides the default implementation to write bundles to shared storage, such as AWS S3, Azure Blob Storage, or a shared NFS volume.

To invalidate in-memory caches across replicas instantly, a Redis Pub/Sub invalidation message is broadcasted when an export completes, triggering replicas to invalidate their local caches and fetch the fresh file from shared storage.

---

## 5. Software Updates and Translation Drift

When self-hosters update the ISLAMU Event Docker image, the new image contains updated binaries and updated embedded translation bundles containing new keys (e.g., `ui.event.new_button`).

### 5.1. Persistent Volume Drift Scenario
If a self-hoster mounts the `App_Data` folder to a persistent host directory (to preserve customized translations), it triggers **Translation Drift**:
1. The new container boots up with updated embedded translations (containing the new `ui.event.new_button` key).
2. The application detects the presence of the host-mounted file (`App_Data/Localization/Bundles/en.json`).
3. Since the application prioritizes local customized files over embedded defaults, it reads the stale local file.
4. Because the stale local file lacks the new key, the user sees the raw key name `ui.event.new_button` in the UI.

### 5.2. Key-by-Key Merged Fallback
To solve this, the `OfflineTranslationProvider` merges resources at load time rather than treating the local file and embedded default files as mutually exclusive.

The lookup function $T_{\text{runtime}}(k)$ for a translation key $k$ is resolved using the following logic:
$$T_{\text{runtime}}(k) = \begin{cases} D_{\text{local}}[k], & \text{if } k \in \text{Keys}(D_{\text{local}}) \\ D_{\text{embedded}}[k], & \text{if } k \notin \text{Keys}(D_{\text{local}}) \land k \in \text{Keys}(D_{\text{embedded}}) \\ k, & \text{otherwise} \end{cases}$$

This ensures that newly introduced UI elements resolve to the defaults shipped with the update, while customized translations on the persistent volume remain preserved.

---

## 6. Database Lookup Translation Strategy

Database lookup tables (e.g., Tags, Categories, Event Types) store entity configurations. To support localization without adding a translations table to the database, we use the following design:

### 6.1. Entity Schema Structure
Translatable lookup entities define three core properties:
```csharp
public class Tag
{
    public int Id { get; set; }
    public string MasterCode { get; set; }
    public string FullName { get; set; }
}
```
* **`Id`**: The database identifier.
* **`MasterCode`**: The immutable, stable identifier (e.g., `FIQH`, `HANAFI`, `CONFERENCE`). This value is used as the key segment in the TMS and offline bundles.
* **`FullName`**: The English name stored in the database. This acts as a low-latency preview/placeholder and a final fallback.

### 6.2. Key Resolution Convention
Lookup translations are mapped in the TMS using the convention:
`lookup.{entity_type}.{MasterCode}.full_name`

For example:
* `lookup.tag.FIQH.full_name` $\rightarrow$ `"Jurisprudence"` (French: `"Jurisprudence islamique"`)
* `lookup.madhab.HANAFI.full_name` $\rightarrow$ `"Hanafi"` (French: `"Hanafite"`)

### 6.3. Low-Latency UX Pattern
To avoid layout shifts and API latency during rendering, the application implements a progressive hydration model:

1. **Database Retrieve**: The entity list is retrieved from the DB.
2. **Immediate UI Render (Placeholder)**: The UI immediately renders the `FullName` property (which is English and already available). This ensures instant loading.
3. **Asynchronous Resolution**: The Blazor client's `ITranslationService` requests the translation for the active culture using the `MasterCode` (e.g., `lookup.tag.FIQH.full_name`).
4. **Hydrate UI**: Once resolved from cache or API, the UI updates the label with the translated text.
5. **English Overrides**: Even when the active culture is English (`en`), the UI still queries the localization API. This is because administrators may have updated the English translations inside Tolgee/Weblate since the last DB migration. If no override exists, the system falls back to the database-persisted `FullName`.

---

## 7. Admin Tooling and CI/CD Integration for Self-Hosters

Self-hosters can choose between full localization platforms or light command-line tools.

### 7.1. Tolgee, Weblate, and LocalizationManager (LRM)
The table below compares the footprint of Tolgee, Weblate, and **LocalizationManager (LRM)** (a lightweight CLI tool that operates on local files and can auto-translate using AI):

| Feature | Tolgee (v2) | Weblate (v5) | LocalizationManager (LRM) |
| :--- | :--- | :--- | :--- |
| **Infrastructure** | Docker container + PostgreSQL. | PostgreSQL, Redis, Celery, Python environment. | Single self-contained binary (zero dependencies). |
| **Integration** | REST API & Tolgee CLI (push/pull). | Git-centric (VCS push/pull) & REST API. | Local CLI & interactive terminal UI (TUI). |
| **Workflows** | In-context editor overlay and Figma plugin. | Continuous localization bound to Git branches. | CLI-driven workflows for developer automation. |
| **Formats** | JSON, XLIFF, CSV, XLSX, RESX. | RESX, PO, properties, JSON, etc. | RESX, JSON, i18next, Android XML, PO. |
| **AI Integration** | Built-in MT and AI Playground (Enterprise). | Translation Memory & suggestions. | Auto-translation via OpenAI, Claude, or Ollama. |

### 7.2. Native Weblate Git Integration
Weblate supports a Git-centric model where the repository is the single source of truth:
1. Developers push new code and keys to the `main` branch.
2. Weblate imports new keys via git webhook.
3. Translators update strings in the Weblate Web UI.
4. Weblate commits and pushes translations back to a dedicated branch (e.g. `l10n/weblate`).
5. A pull request is merged, and the CI/CD pipeline builds the new Docker image containing the updated embedded JSON bundles.

---

## 8. Architectural Recommendations

1. **Standardize on Dynamic JSON**: Avoid Satellite Assemblies in Blazor WASM to eliminate browser reloads and reduce startup payloads.
2. **Secure XML Pipelines**: If XML/RESX files are processed, enforce strict XML parser limits (disabling DTD and external entities) to prevent XXE vulnerabilities (CVE-2026-32251).
3. **Use the `IBundleFileWriter` Seam for HA**: Ensure production deployments swap out the local disk writer for a distributed storage adapter (e.g. S3).
4. **Enforce Key-by-Key Merging**: Prevent translation drift by merging local disk bundles with embedded assembly resources at runtime.
5. **Implement Low-Latency Hydration**: Render the database-persisted `FullName` immediately, then dynamically swap it with the cache-resolved translation.
