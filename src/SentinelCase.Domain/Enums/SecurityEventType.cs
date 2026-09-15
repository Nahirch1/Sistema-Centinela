namespace SentinelCase.Domain.Enums;

public enum SecurityEventType
{
    FailedLoginAttempt = 1,
    SuccessfulLoginFromNewLocation = 2,
    SuspiciousProcessExecution = 3,
    CriticalFileModified = 4,
    FirewallRuleChanged = 5,
    Other = 99
}
