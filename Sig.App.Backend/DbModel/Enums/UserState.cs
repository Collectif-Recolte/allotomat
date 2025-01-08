namespace Sig.App.Backend.DbModel.Enums
{
    public enum UserState
    {
        Active,                     // The user is active.
        InactivePendingDeletion,    // The user is inactive, an email was sent 4 weeks before the scheduled deletion.
        ReminderSentPendingDeletion // A reminder email was sent 2 weeks before the account deletion.
    }
}
