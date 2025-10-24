using System.Collections.Concurrent;
using Net.Contracts;

public interface IWalletService
{
    Task<BalanceDto> GetAsync(string userKey);
    Task<BalanceDto> GrantBonusAsync(string userKey, long amount, string reason);
    Task<BalanceDto> SpendAsync(string userKey, long amount, string reason);
}

public class InMemoryWalletService : IWalletService
{
    private readonly ConcurrentDictionary<string, (long gold, long bonus)> _data
        = new ConcurrentDictionary<string, (long gold, long bonus)>();

    public Task<BalanceDto> GetAsync(string userKey)
    {
        var (g, b) = _data.GetOrAdd(userKey, _ => (0, 0));
        return Task.FromResult(new BalanceDto(g, b, g + b));
    }

    public Task<BalanceDto> GrantBonusAsync(string userKey, long amount, string reason)
    {
        _data.AddOrUpdate(userKey,
            _ => (0, amount),
            (_, old) => (old.gold, old.bonus + amount));

        var (g, b) = _data[userKey];
        return Task.FromResult(new BalanceDto(g, b, g + b));
    }

    public Task<BalanceDto> SpendAsync(string userKey, long amount, string reason)
    {
        _data.AddOrUpdate(userKey, _ => (0, 0), (_, old) =>
        {
            long left = amount;

            long useBonus = Math.Min(old.bonus, left);
            long bonus = old.bonus - useBonus;
            left -= useBonus;

            if (left > 0)
            {
                if (old.gold < left) throw new Exception("insufficient balance");
                return (old.gold - left, bonus);
            }
            return (old.gold, bonus);
        });

        var (g, b) = _data[userKey];
        return Task.FromResult(new BalanceDto(g, b, g + b));
    }
}