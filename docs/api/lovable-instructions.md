# Wiring the published EventPlug UI to the EventPlug API

The prompts below are for the Lovable project behind https://event-plug-ui.lovable.app/. Paste them **one at a time, in order**, and check the app after each one.

Attach [`eventplug-api.swagger.json`](./eventplug-api.swagger.json) to the Lovable chat (or paste its contents) before Prompt 0. Every prompt refers to operations by their `operationId` in that file.

- API base URL: `https://eventplug-api.onrender.com/api/v1`
- Swagger UI: https://eventplug-api.onrender.com/swagger
- Login: demo persona login (no passwords). Auth0 can replace it later without changing the screens.

---

## Prompt 0: Ground rules (paste first)

```
I'm connecting this app to a real backend. The OpenAPI spec is attached (eventplug-api.swagger.json).

Ground rules for this and every following prompt:
1. DO NOT change the visual design, layout, copy, colours, spacing, components, or navigation. The UI is final. Only replace hard-coded/static data with live API data and wire buttons/forms to API calls.
2. If a screen, card, field or button has no matching API operation, leave it exactly as it is with its current static data, and add a code comment `// STATIC: no API yet` above that data.
3. Keep loading and empty states visually minimal: reuse existing skeleton/placeholder styles if present, otherwise show the existing layout with subtle placeholders. Do not add new banners or pages.
4. Show API errors with the app's existing toast component. The API error body is { code, message, fieldErrors? }; show `message`, and put `fieldErrors[field]` under the matching form input.
5. Conventions from the API:
   - Lists come back as { items, total }.
   - Money is a decimal string like "1250.00". Send money as strings too. Format it with the same "$" formatting the UI already uses.
   - Dates are "YYYY-MM-DD"; timestamps are ISO 8601 UTC.
   - Enums are strings (see the spec).
   - PATCH: only send fields that changed; omitted fields are left unchanged.
6. Use TanStack Query for all reads (one query key per resource) and invalidate the related queries after every mutation.

Don't change anything yet. Reply with a short plan of which files hold the static data for each page.
```

## Prompt 1: API client and demo login (the "View as" dropdown)

```
Create src/lib/api.ts:
- const API_BASE = "https://eventplug-api.onrender.com/api/v1"
- an apiFetch(path, options) helper that:
  - adds "Authorization: Bearer <token>" from the stored session
  - adds "X-Org-Id: <orgId>" from the stored session when present
  - sends and parses JSON; returns null for 204
  - throws an ApiError { status, code, message, fieldErrors } for non-2xx responses
  - uses a 90-second timeout (the free server can take ~60s to wake up)
- on 401: clear the session and fall back to the default persona (see below)

Session (store in localStorage under "eventplug.session"): { accessToken, expiresAt, orgId, user }.

Wire the existing sidebar "View as" persona dropdown to the API WITHOUT changing how it looks:
- Populate its options from listDemoUsers (GET /auth/demo-users). Each item has persona, name, orgRole, description and teams[{ teamName, role }]. Use `name` as the label, and use the description/teams text wherever the dropdown currently shows a subtitle.
- Selecting an option calls demoLogin (POST /auth/demo-login, body { persona }) and saves the response as the session, then invalidates all queries.
- On first load, if there is no valid session (missing, or expiresAt in the past), log in automatically as persona "planner".
- After login, load getMe (GET /me) and expose it through a useCurrentUser() hook: { id, name, email, orgRole, organization, teams[{ teamId, teamName, role }] }. Replace any hard-coded current-user name/avatar initials with this data.

Permission helpers, used to hide or disable existing buttons (don't add new UI for this):
- isAdmin = me.orgRole === "Admin"
- canEditEvent(event) = isAdmin || me.teams has { teamId: event.owningTeamId, role: "Planner" }
- canCreateEvent = isAdmin || me.teams has any role "Planner"
```

## Prompt 2: Value mappings (shared helpers)

```
The UI wording differs from the API's enum values. Create src/lib/mappings.ts and use it everywhere, so the UI text stays exactly as it is now.

Event status (API -> UI badge):
- Draft -> "Draft"
- Planning -> "In review"
- Confirmed -> "Approved"
- Complete -> "Complete" (use the "Approved" badge style)
- Cancelled -> "Cancelled" (use the "Draft" badge style)
UI -> API, when filtering or saving: "Draft" -> Draft, "In review" -> Planning, "Approved" -> Confirmed.

Event scope (API -> UI): Team -> "Team", Department -> "Department", CompanyWide -> "Company-wide".

Guest RSVP (API -> UI): Yes -> "Accepted", Invited -> "Pending", No -> "Declined", Maybe -> "Pending".
UI filter -> API rsvpStatus: Accepted -> Yes, Pending -> Invited, Declined -> No (All -> omit the parameter).

Team role (API -> UI role dropdown): Planner -> "Team lead", Approver -> "Coordinator", Viewer -> "Member".
UI -> API: "Team lead" -> Planner, "Event host" -> Planner, "Coordinator" -> Approver, "Member" -> Viewer.

Budget category type (API -> UI label): Venue -> "Venue & AV", Catering -> "Catering", Drinks -> "Drinks", Entertainment -> "Entertainment", Decor -> "Materials & swag", Contingency -> "Contingency", Other -> "Other".

Money: parse API decimal strings with Number(); format with the UI's existing currency formatter.
```

## Prompt 3: Event routes use IDs

```
Event pages currently use slugs (e.g. /events/marketing-offsite and /events/marketing-offsite/edit). Switch them to the event's UUID from the API: /events/:eventId and /events/:eventId/edit. Keep the same page components, tabs and layout. Update every link that points at an event (dashboard cards, calendar entries, My Events rows, budget table rows).

Create hooks:
- useEvents(params) -> listEvents (GET /events?from=&to=&status=&teamId=), returns { items, total } of EventResponse
- useEvent(eventId) -> getEvent (GET /events/{eventId})
- useTeams() -> listTeams (GET /teams)

EventResponse fields: id, title, eventDate, endDate, location, status, scope, guestCountEstimate, budgetAmount, budgetBand, styleNotes, isShared, owningTeamId, teams[{ teamId, teamName, isOwningTeam }], createdBy, createdByName, createdAt.
The event's "team" = the item in teams where isOwningTeam is true.
```

## Prompt 4: Dashboard

```
Wire the Dashboard to the API. Keep the layout identical.

- Date header: keep as is (it's today's date).
- Upcoming events count and cards: useEvents({ from: today }), sorted by eventDate, excluding status Cancelled. Each card shows:
  - owning team name (from teams[]); keep the existing "· type" text static
  - status badge (mapping from Prompt 2)
  - eventDate (and endDate if present) in the existing date format
  - location as the venue
  - "<N> invited": getGuestSummary(eventId).total; if that's not loaded yet, use guestCountEstimate
  - "Hosted by <first name of createdByName>"
  - clicking the card opens /events/{id}
- "People coming together": the sum of guestCountEstimate across upcoming events. "<n> teams" = the number of distinct owning teams among them. Keep "· one shared calendar" as is.
- Planned budget "this quarter": the sum of budgetAmount for events whose eventDate falls in the current calendar quarter. Keep the "of $40,000" quarterly allocation STATIC (no API yet).
- "Next steps" and the "Better, every time" series card: leave STATIC.
```

## Prompt 5: Org Calendar and My Events

```
Org Calendar:
- When the visible month changes, call useEvents({ from: first day of the visible grid, to: last day of the visible grid }) and place each event on eventDate, spanning to endDate if set.
- The My Team / My Department / Whole Company switch:
  - My Team: pass teamId = the current user's first team (me.teams[0].teamId); if they have several teams, merge results for each teamId.
  - My Department: there are no departments yet; behave the same as Whole Company (no teamId).
  - Whole Company: no teamId filter.
- Clicking an event opens /events/{id}.

My Events:
- List events the current user belongs to: for each team in me.teams, call useEvents({ teamId }), merge, de-duplicate by id, and sort by eventDate.
- Each row: title; "date · location · owning team name"; status badge via the mapping.
- Any existing status filter maps UI -> API status with the Prompt 2 mapping.
```

## Prompt 6: New / Edit event form

```
Wire the New Event form (opened via /?new=true) and the Edit Event page (/events/:eventId/edit) to the API, keeping the form's look identical.

Field mapping (form -> API):
- Event name -> title (required)
- Event scope (Team / Department / Company-wide) -> scope (Team | Department | CompanyWide)
- Hosting team -> owningTeamId (required). Options come from useTeams(). For non-admins, only offer teams where me.teams has role Planner.
- Start date -> eventDate ("YYYY-MM-DD", required)
- End date -> endDate ("YYYY-MM-DD", optional, must be on or after the start date)
- Venue -> location (required)
- Estimated budget (USD) -> budgetAmount, sent as a decimal string, e.g. "15500.00"
- Recurring toggle: STATIC (no API yet). Keep it visible but do not send it.
- The event code (e.g. "EVT-2024-MKT-089"): STATIC.

Save event:
- New: createEvent (POST /events) with status "Draft". On 201, navigate to /events/{id}.
- Edit: load with useEvent(eventId) to prefill, then updateEvent (PATCH /events/{eventId}) with only the changed fields. On success, navigate back to /events/{id}.
- On 400, show fieldErrors under the matching inputs (keys: title, eventDate, endDate, location, owningTeamId, scope, budgetAmount).
- Hide the Edit entry point when !canEditEvent(event).

Discard changes: reset the form to the loaded values (or close it for a new event).
"View cost breakdown": link to the event's Budget tab.
```

## Prompt 7: Event detail: header and Overview tab

```
On /events/:eventId, load useEvent(eventId) and replace the static event data in the header and Overview tab:
- Title, status badge, date (eventDate, with endDate if set). Say "One-day gathering" when there is no endDate or it equals eventDate; otherwise show "<n>-day gathering".
- Venue = location
- "<N> invited" = getGuestSummary(eventId).total
- Scope · team = scope label (Prompt 2) + " · " + owning team name
- Estimated budget = budgetAmount
- "Your host" = createdByName (initials from the name)
- "Submit for approval" button: if status is Draft, call updateEvent with { status: "Planning" } (shown as "In review"), then refresh. Hide it when the status isn't Draft or !canEditEvent(event).
- "What's next", the "Part of a recurring series" card, and the Readiness and Activity sub-tabs: STATIC.
- If the event has a delete action in an existing menu, wire it to deleteEvent (DELETE /events/{eventId}) behind the existing confirm, then go to My Events. Don't add a new delete button.

Tabs Communications, Tasks and Series: leave fully STATIC.
```

## Prompt 8: Guest List & RSVP tab

```
Wire the Guest List & RSVP tab:
- Load listGuests (GET /events/{eventId}/guests?rsvpStatus=). The All / Accepted / Pending / Declined filter maps to rsvpStatus (Prompt 2). Search filters client-side by name or email.
- Counts on the filter chips come from getGuestSummary: { total, invited, yes, no, maybe }. Pending = invited + maybe.
- Rows: name and email; Response = RSVP label (Prompt 2). The Team column is STATIC (guests have no team in the API yet); show "—" or keep the current placeholder style.
- The Response control, if it's a dropdown or toggle: updateGuest (PATCH /events/{eventId}/guests/{guestId}) with { rsvpStatus }.
- Add guest: createGuest (POST /events/{eventId}/guests) with { name, email, rsvpStatus: "Invited" }. On 409 (duplicate email), show the message as a toast.
- Remove: deleteGuest (DELETE /events/{eventId}/guests/{guestId}).
- Upload directory: parse the chosen CSV client-side (columns name, email, plus optional dietaryNotes and accessNotes), then call bulkCreateGuests (POST /events/{eventId}/guests/bulk) with { guests: [...] }. Show a toast like "Added X, skipped Y" and list any errors[{ row, message }] in the toast.
- Nudge buttons: STATIC (no API yet).
- Hide or disable add/remove/upload when !canEditEvent(event).
After every mutation, invalidate both the guest list and the guest summary.
```

## Prompt 9: Agenda (run sheet) tab

```
Wire the Agenda tab's run sheet:
- Day tabs: load listRunSheetItems (GET /events/{eventId}/run-sheet) without a day param to get all items, and derive the day tabs from the distinct `day` values (1-based). If there are none, show Day 1. Selecting a tab filters items by day client-side.
- The header "<n> day · <m> sessions" comes from those counts.
- Rows, sorted by sortOrder then startTime:
  - time range = startTime–endTime, shown in local time (the API returns UTC ISO timestamps)
  - title
  - owner · duration: ownerName, or "Everyone" when ownerUserId is null; duration = endTime − startTime (e.g. "45 min")
- Add session: createRunSheetItem (POST /events/{eventId}/run-sheet) with { title, day, startTime, endTime, ownerUserId?, sortOrder }. Build startTime and endTime as ISO UTC timestamps from the event date plus (day − 1) days plus the entered times. Set sortOrder = the current max + 1 for that day. Owner options come from listUsers (GET /users).
- Editing a row (if the UI supports it): updateRunSheetItem (PATCH /events/{eventId}/run-sheet/{itemId}).
- Remove: deleteRunSheetItem (DELETE /events/{eventId}/run-sheet/{itemId}).
- Add day: just add the next day tab client-side. It persists once a session is added to it.
- Hide or disable editing when !canEditEvent(event).
```

## Prompt 10: Event Budget tab

```
Wire the event's Budget tab:
- Load getBudgetSummary (GET /events/{eventId}/budget-summary): { currency, totalAllocated, totalSpent, remaining, categories[{ id, categoryType, allocatedAmount, spentAmount }], teamShares[] }.
- "Total cost to company" = totalAllocated (show totalSpent / remaining wherever the existing card has those slots).
- Cost breakdown by category: one row per categories[] item, labelled via the Prompt 2 category mapping, with the allocated amount and the existing bar/percentage = allocatedAmount / totalAllocated.
- If the UI lets you add or edit a category, use createBudgetCategory (POST /events/{eventId}/budget-categories, body { categoryType, allocatedAmount }), updateBudgetCategory (PATCH) and deleteBudgetCategory (DELETE). A duplicate category type returns 409; show it as a toast.
- If there is an expenses list, use listExpenses (GET /events/{eventId}/expenses?categoryId=) and createExpense (POST /events/{eventId}/budget-categories/{categoryId}/expenses, body { description, amount, vendorId? }).
- The cost split control (Even / Manual / By Headcount), its team list, and "Save split": STATIC (no API yet).
```

## Prompt 11: Teams page

```
Wire the Teams page (/team):
- Teams table: listTeams (GET /teams) returns items[{ id, name, memberCount, myRole }].
  - Team = name; Members = memberCount
  - Team lead = the first member with role "Planner" from listTeamMembers(teamId)
  - Events = the total from listEvents({ teamId })
- Add team (admins only; hide it for others): createTeam (POST /teams) with { name }. A duplicate name returns 409.
- Members table: for each team, call listTeamMembers (GET /teams/{teamId}/members) and flatten the results into rows: Name (+ email), Team (team name), Role dropdown.
  - The Role dropdown shows the Prompt 2 mapping. On change, call updateTeamMember (PATCH /teams/{teamId}/members/{userId}) with { role }. "Event host" saves as Planner.
- Add member: pick a user from listUsers (GET /users?search=), a team, and a role, then call addTeamMember (POST /teams/{teamId}/members) with { userId, role }.
- A remove action, if one exists: removeTeamMember (DELETE /teams/{teamId}/members/{userId}).
- Only admins or Planners of that team can change roles or members; disable the controls otherwise.
- A team must keep at least one Planner. The API returns 409 otherwise; show its message as a toast and revert the dropdown.
```

## Prompt 12: Budget overview page

```
Wire the Budget page (org-wide overview):
- The quarter selector (e.g. "Q4 2026") defines from/to dates. Load listEvents({ from, to }) for that quarter, excluding Cancelled.
- Total cost to company / planned = the sum of budgetAmount across those events.
- The quarterly allocation (e.g. $40,000) is STATIC. Available = allocation − planned.
- By-event table: event title (links to /events/{id}), owning team name, budget = budgetAmount, and status badge (Prompt 2 mapping).
- Any by-team breakdown: group the same events by owning team and sum budgetAmount.
```

## Leave static (no API yet)

My Tasks, the event Tasks tab, Approvals (including the sidebar badge), Communications, Series and the dashboard series card, Readiness, Activity, "Next steps", guest nudges, cost splits, the Recurring toggle, the event code, the guest Team column, and the quarterly allocation figure.

## Troubleshooting

- **The first request takes about a minute.** The free Render server is waking up. Later requests are fast.
- **401 everywhere.** The token has expired (12h). Pick a persona again in "View as".
- **403 on save.** The persona doesn't have permission. Use "Alex Admin" or "Priya Planner".
- **CORS error.** The API allows `https://*.lovable.app` and `https://*.lovableproject.com`. Add a custom domain on Render as another `Cors__AllowedOrigins__<n>` variable.
