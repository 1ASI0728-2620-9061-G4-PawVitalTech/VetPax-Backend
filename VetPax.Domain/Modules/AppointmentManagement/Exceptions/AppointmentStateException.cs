namespace VetPax.Domain.Modules.AppointmentManagement.Exceptions;

public sealed class AppointmentStateException : Exception
{
    public AppointmentStateException(string message)
        : base(message)
    {
    }
}