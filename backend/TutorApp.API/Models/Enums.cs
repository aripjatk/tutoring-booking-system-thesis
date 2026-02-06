namespace TutorApp.API.Models
{
    public enum EventType
    {
        Activation,
        Deactivation
    }

    public enum MeansOfPayment
    {
        Cash,
        BankTransfer,
        BLIK
    }

    public enum ConfirmationStatus
    {
        Unknown,
        Yes,
        No
    }

    public enum NotificationType
    {
        SessionAccepted,
        SessionRejected,
        HomeworkSolutionUploaded,
        MessageReceived,
        HomeworkAssigned,
        SessionCreated
    }
}
