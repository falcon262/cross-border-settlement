namespace Settlement.Payments.Domain;

public enum PaymentStatus
{
    Accepted = 1,
    Settling = 2,
    Settled = 3,
    Failed = 4
}