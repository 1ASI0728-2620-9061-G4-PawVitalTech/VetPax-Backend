namespace VetPax.Application.Modules.AppointmentManagement.Exceptions;

public sealed class AppointmentConflictException : Exception
{
    public AppointmentConflictException(string message)
        : base(message)
    {
    }
}