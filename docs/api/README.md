# EventPlug API: front-end handoff (Lovable)

The API contract is in [`eventplug-api.swagger.json`](./eventplug-api.swagger.json) (OpenAPI 3.0, 51 operations). Give Lovable that file along with the settings below.

## Base URL

All endpoints live under `/api/v1`. The swagger paths already include that prefix, so the front end only needs the host:

| Environment | Base URL |
|---|---|
| Local (`dotnet run`) | `http://localhost:5144` |
| Docker Compose | `http://localhost:8080` |
| Deployed | _fill in_ |

Live Swagger UI is served at `/swagger` when the API runs in the Development environment.

## Auth0

The API validates Auth0 access tokens (RS256) issued for its audience.

| Setting | Value |
|---|---|
| Auth0 domain | _your tenant, e.g. `eventplug.au.auth0.com`_ |
| Auth0 audience (API identifier) | _e.g. `https://api.eventplug`_ |
| SPA client ID | _the Auth0 Single Page Application client ID_ |

Front-end setup (`@auth0/auth0-react`):

```ts
<Auth0Provider
  domain={AUTH0_DOMAIN}
  clientId={AUTH0_CLIENT_ID}
  authorizationParams={{ redirect_uri: window.location.origin, audience: AUTH0_AUDIENCE }}
>
```

Request the token with the **audience** set. Without it, Auth0 returns an opaque token, and the API rejects it with 401.

In the Auth0 SPA application settings, add the Lovable preview and production URLs to **Allowed Callback URLs**, **Allowed Logout URLs** and **Allowed Web Origins**.

Backend configuration (`backend/EventPlug.Api/appsettings.json`, or environment variables `Auth0__Domain`, `Auth0__Audience`):

```json
"Auth0": { "Domain": "eventplug.au.auth0.com", "Audience": "https://api.eventplug" }
```

## Testing without Auth0 (DevAuth)

For local testing against a single organisation you can skip Auth0 entirely. In `backend/EventPlug.Api/appsettings.Development.json`:

```json
"DevAuth": {
  "Enabled": true,
  "Subject": "<auth0_subject of an existing users row>",
  "OrgId": "<organisation UUID>"
}
```

With this on:

- Every request runs as that user. No `Authorization` header is needed.
- `X-Org-Id` is optional and falls back to `OrgId`.
- Permissions still apply, so pick an Admin user if you want full access.
- It only works when `ASPNETCORE_ENVIRONMENT=Development`. The API refuses to start if it is enabled anywhere else.

Set `"Enabled": false` to go back to real Auth0 tokens.

## Required headers on every `/api/v1` call

```
Authorization: Bearer <Auth0 access token>
X-Org-Id: <organisation UUID>
```

- A missing or invalid token returns `401 unauthorized`.
- A missing or invalid `X-Org-Id` returns `400 missing_org`.
- A signed-in user with no `users` row in that organisation returns `403 not_a_member`.

**Getting the org ID.** No endpoint returns it yet; every call, including `/me`, needs it. For the MVP, set it as a front-end config value (e.g. `VITE_ORG_ID`) for the single organisation.

**User provisioning.** Users are not created automatically on first login. A row in `users` with a matching `auth0_subject` (the token's `sub`) must already exist in the organisation.

## CORS

Allowed browser origins come from `Cors:AllowedOrigins` (or `Cors__AllowedOrigins__0`, `__1` and so on). Add the exact Lovable origins. Wildcards are not supported.

```json
"Cors": { "AllowedOrigins": [ "http://localhost:5173", "https://<project>.lovable.app" ] }
```

## Conventions

- **IDs** are UUIDs.
- **Enums** are strings. The allowed values are listed in the swagger `components.schemas`.
- **Money** is a decimal string with up to 2 places, e.g. `"1250.00"`. The API also accepts JSON numbers on input.
- **Dates** use `YYYY-MM-DD`, for example `eventDate` and `endDate`.
- **Timestamps** are ISO 8601. Run sheet times are stored and returned in UTC.
- **Lists** return `{ "items": [...], "total": n }`.
- **Errors** return `{ "code", "message", "fieldErrors"? }`. The status codes are:
  - `400` validation (with `fieldErrors` keyed by camelCase field name)
  - `401` not signed in
  - `403` no permission
  - `404` not found
  - `409` conflict, e.g. duplicate or still in use
- **PATCH** works like this: omitted or `null` fields are left unchanged.
- **Create** returns `201` with the created object. **Delete** returns `204`.

## Permissions (shown in each operation's summary)

| Who | Can |
|---|---|
| Org Admin | Everything. Admins are the only ones who can create, rename or delete teams and delete vendors. |
| Planner on the event's owning team | Edit the event, its teams, guests, run sheet, budget, expenses and vendor links. |
| Planner on any team | Create events for that team, and create or edit vendors. |
| Any org member | Read everything in the organisation. |

`GET /api/v1/me` returns the user's org role and their team memberships, so the UI can show or hide actions.

## Suggested screen-to-endpoint map

| Screen | Endpoints |
|---|---|
| App shell / profile | `getMe` |
| Events list / calendar | `listEvents` (`from`, `to`, `status`, `teamId`) |
| Create / edit event | `createEvent`, `updateEvent`, `listTeams` |
| Event: guests | `listGuests`, `getGuestSummary`, `createGuest`, `bulkCreateGuests`, `updateGuest`, `deleteGuest` |
| Event: run sheet | `listRunSheetItems` (`day`), `createRunSheetItem`, `updateRunSheetItem`, `deleteRunSheetItem` |
| Event: budget | `getBudgetSummary`, `listBudgetCategories`, `listExpenses`, `createExpense`, `updateExpense` |
| Event: vendors | `listEventVendors`, `addEventVendor`, `updateEventVendor`, `listVendors` |
| Teams admin | `listTeams`, `createTeam`, `listTeamMembers`, `addTeamMember`, `listUsers` |

## Not in this version

Approvals, expense team splits, quote requests, AI drafts, guest nudges, org-wide budget overview, departments, series, tasks and announcements.

## Regenerating the swagger file

```bash
cd backend/EventPlug.Api
ASPNETCORE_ENVIRONMENT=Development Auth0__Domain=x Auth0__Audience=y dotnet run --urls http://localhost:5199
# in another terminal
curl -s http://localhost:5199/swagger/v1/swagger.json | python3 -m json.tool > ../../docs/api/eventplug-api.swagger.json
```

## Deploying (Render)

`render.yaml` at the repo root is a Render Blueprint: a Docker web service (`eventplug-api`) plus a managed Postgres (`eventplug-db`), both in Singapore.

- Migrations run automatically on startup (`Database__MigrateOnStartup=true`).
- Swagger is served at `/swagger` (`Swagger__Enabled=true`).
- CORS allows `https://*.lovable.app`, `https://*.lovableproject.com` and `http://localhost:5173`. Add more via `Cors__AllowedOrigins__N`.
- The database URL Render provides (`postgresql://…`) is accepted as-is.
- DevAuth cannot run in production, so callers must send a real Auth0 token and an `X-Org-Id` header.
- After the first deploy, insert the organisation and your user (with your Auth0 `sub` as `auth0_subject`) into the cloud database. Users are not auto-provisioned.
- On the free plan the service sleeps when idle, so the first request after a pause takes around 30–60 seconds.
