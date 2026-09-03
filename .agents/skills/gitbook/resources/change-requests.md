<!-- ABOUTME: End-to-end authoring and review lifecycle for GitBook Change Requests via REST API or MCP. -->
<!-- ABOUTME: Covers CR discovery, content pushing, diff inspection, comment handling, verdicts, and preview link generation. -->

# GitBook Change Requests: Authoring & Review Lifecycle

A guide for driving the entire GitBook documentation review loop programmatically using the GitBook REST API or MCP server. Covers both authoring (creating CRs, pushing page updates, requesting review, addressing comments) and reviewing (discovering open CRs, summarizing diffs, leaving comments, submitting verdicts).

## Authentication & Shell Helper

All requests authenticate against `https://api.gitbook.com/v1` with a Bearer token stored in `GITBOOK_TOKEN` (in `.env` or environment). Never print or commit tokens.

Define this shell helper for REST calls:
```bash
set -a; [ -f .env ] && . ./.env; set +a
gbapi() {
  local method="$1" apipath="$2"; shift 2
  curl -sS --fail-with-body -X "$method" \
    "https://api.gitbook.com/v1${apipath}" \
    -H "Authorization: Bearer ${GITBOOK_TOKEN}" \
    -H "Content-Type: application/json" "$@"
}
```
Every response is JSON; process with `jq`. If `gbapi` exits non-zero, inspect the API error body.

## Mandatory Two-Link Rule

Whenever a change request is created or content is pushed to it (via REST or MCP), **you must report two distinct links**:
1. **The Editor / Diff Link**: `urls.app` from the change-request response.
2. **The Rendered Site Preview Link**: Obtained from the underlying `Site` object (`urls.published` or `urls.preview`) with **`/~/changes/<number>/` appended**.

A bare site URL displays the live site content, NOT the change request. Scope preview links with `/~/changes/<number>/`:
```bash
ORG=$(gbapi GET "/spaces/<spaceId>" | jq -r .organization)
SITE=$(gbapi GET "/orgs/$ORG/sites" | jq -r '.items[].id' | head -n 1)
BASE=$(gbapi GET "/orgs/$ORG/sites/$SITE" | jq -r 'if .visibility == "public" and .urls.published then .urls.published else .urls.preview end')
NUM=$(gbapi GET "/spaces/<spaceId>/change-requests/<crId>" | jq -r .number)
echo "${BASE%/}/~/changes/${NUM}/"
```

## Confirmation Gates

Pause and get explicit user approval before executing any public or state-changing call:
- `POST …/change-requests` (creates a CR)
- `POST …/requested-reviewers` (assigns reviewers and triggers notifications)
- Slack webhook notifications (public posting)
- `PUT …/comments/<id>` with `{"resolved":true}` (closes discussion)
- `POST …/reviews` with `{"status":"approved"|"changes-requested"}` (records formal verdict)

---

## Authoring Workflow

### 1. Create a Change Request
```bash
gbapi POST "/spaces/<spaceId>/change-requests" \
  --data '{"subject":"Update webhook retry documentation"}' \
  | jq '{id, number, url: .urls.app}'
```

### 2. Push Content Safely (Markdown Round-Trip Rules)
Content changes are pushed in sequential batches via `POST …/content`:
```json
{
  "changes": [
    {
      "operation": "update_page",
      "page": "<pageId>",
      "document": { "markdown": "...body content..." }
    },
    {
      "operation": "insert_page",
      "title": "New Guide",
      "into": "<parentPageId>",
      "document": { "markdown": "...body content..." }
    }
  ]
}
```

#### Safe Markdown Round-Trip Checks
1. **Strip Duplicate Leading `# <Title>`**: `GET …/page?format=markdown` emits the title as an H1 heading, but pushing it back inside body markdown creates a duplicate header in the editor. Push only the body *below* the title.
2. **Collapse Multi-Line Integration Blocks**: Multi-line `{% @mermaid/diagram %}` blocks get re-escaped into literal text (`\{% ... %}`). Collapse them to single-line strings before pushing (e.g., semicolon-separated Mermaid).
3. **Verify Landed Content**: Always re-fetch the page from the CR after pushing (`GET …/change-requests/<crId>/content/page/<pageId>?format=markdown`) to verify formatting.

### 3. Request Reviewers & Notify Slack
```bash
gbapi POST "/spaces/<spaceId>/change-requests/<crId>/requested-reviewers" \
  --data '{"users":["<userId>"]}' | jq '.'
```
If configured, dispatch a Slack notification using `SLACK_WEBHOOK_URL` linking the diff and rendered preview.

---

## Reviewing Workflow

### 1. Discover Change Requests
List open CRs across an organization or single space:
```bash
# In a single space (status parameter is required)
gbapi GET "/spaces/<spaceId>/change-requests?status=open" | jq '.items'

# Across an organization assigned to current user
ME=$(gbapi GET /user | jq -r .id)
gbapi GET "/orgs/<orgId>/change-requests?status=open&requestedReviewer=$ME" | jq '.items'
```
*Note: Omitting `status` returns an empty list. Default discovery to `status=open`.*

### 2. Summarize What Changed
1. **Structural Summary**: `GET /spaces/<spaceId>/change-requests/<crId>/changes` returns `.changes` containing `page_created` and `page_edited` entries.
2. **Native Diff of Record**: Always direct users to `urls.app` for the word-level, syntax-aware visual diff.
3. **Prose Summary**: Diff CR markdown against base markdown client-side to summarize substantive edits.

### 3. Commenting & Verdicts
- **Leave a Comment** (Gate):
  ```bash
  gbapi POST "/spaces/<spaceId>/change-requests/<crId>/comments" \
    --data '{"body":{"markdown":"Consider clarifying the exponential backoff formula."},"page":"<pageId>"}' | jq '.'
  ```
- **Submit Verdict** (Gate):
  ```bash
  gbapi POST "/spaces/<spaceId>/change-requests/<crId>/reviews" \
    --data '{"status":"approved","comment":{"markdown":"LGTM!"}}' | jq '.'
  # status must be "approved" or "changes-requested"
  ```

---

## The Collaborative Comment Loop

Change requests often contain two distinct sources of comments:
1. **Human Comments (`postedBy.id != "gitbook:agent"`)**: Authoritative. Address these directly.
2. **GitBook Agent Comments (`postedBy.id == "gitbook:agent"`)**: Advisory. Evaluate recommendations, fix valid issues, but leave out-of-scope items open for human triage.

### Closing the Loop: Reply Before Resolve
The GitBook API permits resolving comments unconditionally without a reply. **Enforce the reply-first invariant**:
1. Address the feedback and push updated content.
2. Post a reply detailing the fix:
   ```bash
   gbapi POST "/spaces/<spaceId>/change-requests/<crId>/comments/<commentId>/replies" \
     --data '{"body":{"markdown":"Updated retry policy to specify 5 attempts with jitter in latest revision."}}' | jq '.'
   ```
3. Resolve the comment (Gate):
   ```bash
   gbapi PUT "/spaces/<spaceId>/change-requests/<crId>/comments/<commentId>" \
     --data '{"resolved":true}' | jq '.'
   ```

## Related Resources

- [api-cheatsheet.md](api-cheatsheet.md) — Complete REST endpoint reference and JSON schemas.
- [git-sync-previews.md](git-sync-previews.md) — Preview links for Git-synced branches.
- [env.example](env.example) — Reference environment configuration.
