namespace Net.Contracts
{
    public record SpendGoldReq(long Amount, string Reason);
    public record GrantBonusReq(long Amount, string Reason);
}