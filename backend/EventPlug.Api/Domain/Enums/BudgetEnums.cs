namespace EventPlug.Api.Domain.Enums;

public enum CategoryType
{
    Venue,
    Catering,
    Drinks,
    Entertainment,
    Decor,
    Contingency,
    Other
}

public enum ExpenseStatus
{
    Pending,
    Approved,
    Rejected,
    PaidManual
}

public enum ApprovalStatus
{
    Pending,
    Approved,
    Rejected
}

public enum ApprovalAction
{
    Requested,
    Approved,
    Rejected,
    Commented
}
