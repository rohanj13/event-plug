namespace EventPlug.Api.Domain.Enums;

public enum EventStatus
{
    Draft,
    Planning,
    Confirmed,
    Complete,
    Cancelled
}

public enum SplitMethod
{
    Even,
    Headcount,
    Custom
}

public enum RsvpStatus
{
    Invited,
    Yes,
    No,
    Maybe
}
