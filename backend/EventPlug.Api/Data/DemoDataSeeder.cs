using EventPlug.Api.Domain.Entities;
using EventPlug.Api.Domain.Enums;
using EventPlug.Api.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace EventPlug.Api.Data;

/// <summary>
/// Creates the sample organisation used by DemoAuth: two teams, one persona per role and a couple
/// of events with guests, run sheet, budget and vendors. Runs once; skipped if the org exists.
/// </summary>
public static class DemoDataSeeder
{
    public sealed record Persona(Guid Id, string Key, string Name, string Email, OrgRole OrgRole, string Description);

    public static readonly IReadOnlyList<Persona> Personas = new[]
    {
        new Persona(Guid.Parse("0d3e6a10-0000-4000-8000-0000000000a1"), "admin", "Alex Admin", "admin@acme-demo.test", OrgRole.Admin,
            "Organisation admin. Can do everything, including managing teams and users."),
        new Persona(Guid.Parse("0d3e6a10-0000-4000-8000-0000000000a2"), "planner", "Priya Planner", "planner@acme-demo.test", OrgRole.Member,
            "Planner on People & Culture. Creates and edits that team's events."),
        new Persona(Guid.Parse("0d3e6a10-0000-4000-8000-0000000000a3"), "approver", "Aaron Approver", "approver@acme-demo.test", OrgRole.Member,
            "Approver on People & Culture. Read access to events; approvals are coming later."),
        new Persona(Guid.Parse("0d3e6a10-0000-4000-8000-0000000000a4"), "viewer", "Vic Viewer", "viewer@acme-demo.test", OrgRole.Member,
            "Viewer on People & Culture. Read-only.")
    };

    public static string SubjectFor(Persona persona) => DemoAuthOptions.SubjectPrefix + persona.Key;

    public static async Task SeedAsync(EventPlugDbContext db, Guid orgId, CancellationToken cancellationToken = default)
    {
        if (await db.Organizations.AnyAsync(o => o.Id == orgId, cancellationToken))
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        db.Organizations.Add(new Organization
        {
            Id = orgId,
            Name = "Acme Corp (Demo)",
            SubscriptionTier = SubscriptionTier.Team,
            BillingCycleStart = new DateOnly(today.Year, today.Month, 1),
            CreatedAt = now
        });

        var users = Personas.ToDictionary(p => p.Key, p => new User
        {
            Id = p.Id,
            OrgId = orgId,
            Auth0Subject = SubjectFor(p),
            Name = p.Name,
            Email = p.Email,
            OrgRole = p.OrgRole
        });
        db.Users.AddRange(users.Values);

        var peopleAndCulture = new Team { Id = Guid.NewGuid(), OrgId = orgId, Name = "People & Culture", CreatedAt = now };
        var engineering = new Team { Id = Guid.NewGuid(), OrgId = orgId, Name = "Engineering", CreatedAt = now };
        db.Teams.AddRange(peopleAndCulture, engineering);

        db.TeamMemberships.AddRange(
            Membership(orgId, peopleAndCulture, users["admin"], TeamRole.Planner),
            Membership(orgId, peopleAndCulture, users["planner"], TeamRole.Planner),
            Membership(orgId, peopleAndCulture, users["approver"], TeamRole.Approver),
            Membership(orgId, peopleAndCulture, users["viewer"], TeamRole.Viewer),
            Membership(orgId, engineering, users["admin"], TeamRole.Planner),
            Membership(orgId, engineering, users["viewer"], TeamRole.Viewer));

        var venue = new Vendor { Id = Guid.NewGuid(), OrgId = orgId, Name = "The Glasshouse Venues", Category = "Venue", ContactEmail = "events@glasshouse.example", ContactPhone = "02 9000 1000" };
        var caterer = new Vendor { Id = Guid.NewGuid(), OrgId = orgId, Name = "Feast & Co Catering", Category = "Catering", ContactEmail = "hello@feastco.example" };
        var dj = new Vendor { Id = Guid.NewGuid(), OrgId = orgId, Name = "DJ Spinz", Category = "Entertainment", Notes = "Needs 2 power outlets near the stage." };
        db.Vendors.AddRange(venue, caterer, dj);

        // Event 1: end-of-year party run by People & Culture, shared with Engineering.
        var party = new Event
        {
            Id = Guid.NewGuid(),
            OrgId = orgId,
            Title = "End of Year Party",
            EventDate = today.AddDays(35),
            Location = "The Glasshouse, Sydney",
            GuestCountEstimate = 80,
            BudgetBand = "$10k-$20k",
            StyleNotes = "Garden party theme, smart casual.",
            Status = EventStatus.Planning,
            IsShared = true,
            CreatedBy = users["planner"].Id,
            CreatedAt = now
        };
        db.Events.Add(party);
        db.EventTeams.AddRange(
            new EventTeam { Id = Guid.NewGuid(), OrgId = orgId, EventId = party.Id, TeamId = peopleAndCulture.Id, IsOwningTeam = true, SplitMethod = SplitMethod.Even, ApproverUserId = users["approver"].Id },
            new EventTeam { Id = Guid.NewGuid(), OrgId = orgId, EventId = party.Id, TeamId = engineering.Id, IsOwningTeam = false, SplitMethod = SplitMethod.Even, ApproverUserId = users["admin"].Id });

        db.Guests.AddRange(
            Guest(orgId, party, "Jordan Lee", "jordan.lee@acme-demo.test", RsvpStatus.Yes, dietary: "Vegetarian"),
            Guest(orgId, party, "Sam Patel", "sam.patel@acme-demo.test", RsvpStatus.Yes),
            Guest(orgId, party, "Mia Chen", "mia.chen@acme-demo.test", RsvpStatus.Maybe, dietary: "Gluten free"),
            Guest(orgId, party, "Noah Williams", "noah.williams@acme-demo.test", RsvpStatus.Invited),
            Guest(orgId, party, "Olivia Brown", "olivia.brown@acme-demo.test", RsvpStatus.Invited, access: "Wheelchair access"),
            Guest(orgId, party, "Liam Nguyen", "liam.nguyen@acme-demo.test", RsvpStatus.No));

        db.RunSheetItems.AddRange(
            RunSheet(orgId, party, 1, 0, "17:00", "17:45", "Venue setup", "Decor and AV check", users["planner"].Id),
            RunSheet(orgId, party, 1, 1, "18:00", "19:00", "Arrivals and drinks", null, null),
            RunSheet(orgId, party, 1, 2, "19:00", "19:15", "Welcome speech", "CEO thanks the team", users["admin"].Id),
            RunSheet(orgId, party, 1, 3, "19:15", "22:30", "Dinner and DJ", null, null));

        var partyVenue = Category(orgId, party, CategoryType.Venue, 6000m);
        var partyCatering = Category(orgId, party, CategoryType.Catering, 5000m);
        var partyDrinks = Category(orgId, party, CategoryType.Drinks, 3000m);
        var partyEntertainment = Category(orgId, party, CategoryType.Entertainment, 1500m);
        db.BudgetCategories.AddRange(partyVenue, partyCatering, partyDrinks, partyEntertainment);

        db.Expenses.AddRange(
            Expense(orgId, partyVenue, "Venue deposit (50%)", 3000m, venue, ExpenseStatus.Approved, users["planner"], now),
            Expense(orgId, partyCatering, "Canapes and mains for 80", 4800m, caterer, ExpenseStatus.Pending, users["planner"], now),
            Expense(orgId, partyEntertainment, "DJ, 3.5 hours", 1200m, dj, ExpenseStatus.Pending, users["planner"], now));

        db.VendorEventLinks.AddRange(
            new VendorEventLink { Id = Guid.NewGuid(), OrgId = orgId, EventId = party.Id, VendorId = venue.Id, Status = VendorEventStatus.Booked },
            new VendorEventLink { Id = Guid.NewGuid(), OrgId = orgId, EventId = party.Id, VendorId = caterer.Id, Status = VendorEventStatus.QuoteRequested },
            new VendorEventLink { Id = Guid.NewGuid(), OrgId = orgId, EventId = party.Id, VendorId = dj.Id, Status = VendorEventStatus.Contacted });

        // Event 2: a two-day Engineering offsite still in draft.
        var offsite = new Event
        {
            Id = Guid.NewGuid(),
            OrgId = orgId,
            Title = "Engineering Offsite",
            EventDate = today.AddDays(80),
            EndDate = today.AddDays(81),
            Location = "Blue Mountains, NSW",
            GuestCountEstimate = 25,
            BudgetBand = "$5k-$10k",
            Status = EventStatus.Draft,
            IsShared = false,
            CreatedBy = users["admin"].Id,
            CreatedAt = now
        };
        db.Events.Add(offsite);
        db.EventTeams.Add(new EventTeam { Id = Guid.NewGuid(), OrgId = orgId, EventId = offsite.Id, TeamId = engineering.Id, IsOwningTeam = true, SplitMethod = SplitMethod.Even, ApproverUserId = users["admin"].Id });

        db.Guests.AddRange(
            Guest(orgId, offsite, "Ava Martin", "ava.martin@acme-demo.test", RsvpStatus.Yes),
            Guest(orgId, offsite, "Ethan Kim", "ethan.kim@acme-demo.test", RsvpStatus.Invited));

        db.RunSheetItems.AddRange(
            RunSheet(orgId, offsite, 1, 0, "09:00", "11:00", "Coach to Blue Mountains", null, null),
            RunSheet(orgId, offsite, 1, 1, "13:00", "17:00", "Roadmap workshop", null, users["admin"].Id),
            RunSheet(orgId, offsite, 2, 0, "09:00", "12:00", "Hike and team activity", null, null));

        db.BudgetCategories.Add(Category(orgId, offsite, CategoryType.Venue, 4000m));

        await db.SaveChangesAsync(cancellationToken);
    }

    private static TeamMembership Membership(Guid orgId, Team team, User user, TeamRole role) =>
        new() { Id = Guid.NewGuid(), OrgId = orgId, TeamId = team.Id, UserId = user.Id, TeamRole = role };

    private static Guest Guest(Guid orgId, Event evt, string name, string email, RsvpStatus rsvp, string? dietary = null, string? access = null) =>
        new() { Id = Guid.NewGuid(), OrgId = orgId, EventId = evt.Id, Name = name, Email = email, RsvpStatus = rsvp, DietaryNotes = dietary, AccessNotes = access };

    private static BudgetCategory Category(Guid orgId, Event evt, CategoryType type, decimal allocated) =>
        new() { Id = Guid.NewGuid(), OrgId = orgId, EventId = evt.Id, CategoryType = type, AllocatedAmount = allocated, Currency = "AUD" };

    private static Expense Expense(Guid orgId, BudgetCategory category, string description, decimal amount, Vendor vendor, ExpenseStatus status, User createdBy, DateTimeOffset now) =>
        new() { Id = Guid.NewGuid(), OrgId = orgId, BudgetCategoryId = category.Id, Description = description, Amount = amount, VendorId = vendor.Id, Status = status, CreatedBy = createdBy.Id, CreatedAt = now };

    private static RunSheetItem RunSheet(Guid orgId, Event evt, int day, int sortOrder, string start, string end, string title, string? description, Guid? ownerUserId)
    {
        // Times are Sydney local; DST is ignored for demo data.
        var date = evt.EventDate.AddDays(day - 1);
        var sydney = TimeSpan.FromHours(11);
        DateTimeOffset At(string time) => new DateTimeOffset(date.ToDateTime(TimeOnly.Parse(time)), sydney).ToUniversalTime();

        return new RunSheetItem
        {
            Id = Guid.NewGuid(),
            OrgId = orgId,
            EventId = evt.Id,
            Day = day,
            SortOrder = sortOrder,
            StartTime = At(start),
            EndTime = At(end),
            Title = title,
            Description = description,
            OwnerUserId = ownerUserId
        };
    }
}
