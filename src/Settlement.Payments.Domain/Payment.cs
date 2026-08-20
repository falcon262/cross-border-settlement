namespace Settlement.Payments.Domain;

public sealed class Payment
{
    private Payment() { }   // EF Core materialisation only

    private Payment(
        Guid id, string reference, decimal amount, string currency,
        string sourceCountry, string destinationCountry,
        string idempotencyKey, DateTimeOffset createdAtUtc)
    {
        Id = id;
        Reference = reference;
        Amount = amount;
        Currency = currency;
        SourceCountry = sourceCountry;
        DestinationCountry = destinationCountry;
        IdempotencyKey = idempotencyKey;
        CreatedAtUtc = createdAtUtc;
        Status = PaymentStatus.Accepted;
    }

    public Guid Id { get; private set; }
    public string Reference { get; private set; } = null!;
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = null!;
    public string SourceCountry { get; private set; } = null!;
    public string DestinationCountry { get; private set; } = null!;
    public PaymentStatus Status { get; private set; }
    public string IdempotencyKey { get; private set; } = null!;
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static Payment Accept(
        string reference, decimal amount, string currency,
        string sourceCountry, string destinationCountry,
        string idempotencyKey, TimeProvider clock)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reference);
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);

        if (amount <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(amount), "A payment amount must be positive.");

        currency           = NormaliseIsoCode(currency, 3, nameof(currency));
        sourceCountry      = NormaliseIsoCode(sourceCountry, 2, nameof(sourceCountry));
        destinationCountry = NormaliseIsoCode(destinationCountry, 2, nameof(destinationCountry));

        if (sourceCountry == destinationCountry)
            throw new ArgumentException(
                "A cross-border payment requires two different countries.",
                nameof(destinationCountry));

        return new Payment(
            Guid.CreateVersion7(), reference.Trim(), amount, currency,
            sourceCountry, destinationCountry, idempotencyKey.Trim(), clock.GetUtcNow());
    }

    public void MarkSettling() => Transition(PaymentStatus.Settling, PaymentStatus.Accepted);
    public void MarkSettled()  => Transition(PaymentStatus.Settled,  PaymentStatus.Settling);
    public void Fail()         => Transition(PaymentStatus.Failed,
                                             PaymentStatus.Accepted, PaymentStatus.Settling);

    private void Transition(PaymentStatus to, params PaymentStatus[] allowedFrom)
    {
        if (!allowedFrom.Contains(Status))
            throw new InvalidOperationException(
                $"Cannot move a payment from {Status} to {to}.");
        Status = to;
    }

    private static string NormaliseIsoCode(string value, int length, string paramName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, paramName);
        var code = value.Trim().ToUpperInvariant();
        if (code.Length != length)
            throw new ArgumentException(
                $"Expected a {length}-character ISO code, got '{value}'.", paramName);
        return code;
    }
}