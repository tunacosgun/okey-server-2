using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameServer.Contracts;

namespace GameServer.Services
{
    /// <summary>
    /// Basit bellek-içi tablo, oyuncu ve hazır-durumu yönetimi.
    /// Prod için DB koyabilirsiniz; API yüzeyi aynı kalır.
    /// </summary>
    public class TableService
    {
        private readonly ConcurrentDictionary<string, TableInfo> _tables = new();
        private readonly ConcurrentDictionary<string, string>     _connToTable = new(); // connId -> tableId
        private readonly ConcurrentDictionary<string, string>     _connToPlayer = new(); // connId -> player
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, bool>> _tableReadyMap = new(); // tableId -> (player -> ready)

        // --------- CREATE ----------
        public Task<string> CreateTableSimpleAsync() =>
            CreateTableAsync(new CreateTableReq { Mode = "tournament", Capacity = 4 });

        public Task<string> CreateTableAsync(CreateTableReq req)
        {
            var id = "table-" + Guid.NewGuid().ToString("N")[..8];

            var info = new TableInfo
            {
                Id        = id,
                Mode      = string.IsNullOrWhiteSpace(req.Mode) ? "classic101" : req.Mode,
                Capacity  = req.Capacity <= 0 ? 4 : req.Capacity,
                PlayerCount = 0,
                Players     = new List<string>(),
                IsPrivate   = req.IsPrivate,
                Password    = req.Password ?? "",
                EntryFee    = req.EntryFee,
                IsActive    = true
            };

            _tables[id] = info;
            _tableReadyMap[id] = new ConcurrentDictionary<string, bool>(StringComparer.Ordinal);

            return Task.FromResult(id);
        }

        // --------- LIST ----------
        public Task<List<TableInfo>> ListTablesAsync(ListTablesReq req)
        {
            IEnumerable<TableInfo> q = _tables.Values;

            if (!string.IsNullOrWhiteSpace(req.Mode))
                q = q.Where(t => string.Equals(t.Mode, req.Mode, StringComparison.OrdinalIgnoreCase));

            // İstenirse aktif olmayanları da gösterebilirsiniz; şimdilik aktif olanlar varsayılan.
            q = q.Where(t => t.IsActive);

            var limit = req.Limit <= 0 ? 50 : req.Limit;
            var list = q.Take(limit).ToList();
            return Task.FromResult(list);
        }

        // --------- JOIN / LEAVE ----------
        public Task AddPlayerAsync(string connectionId, string tableId, string player)
        {
            if (!_tables.TryGetValue(tableId, out var table))
                throw new InvalidOperationException("Table not found.");

            lock (table)
            {
                if (!table.Players.Contains(player))
                {
                    table.Players.Add(player);
                    table.PlayerCount = table.Players.Count;
                }
            }

            _connToTable[connectionId]  = tableId;
            _connToPlayer[connectionId] = player;

            // hazır durumu default false
            _tableReadyMap.GetOrAdd(tableId, _ => new ConcurrentDictionary<string, bool>(StringComparer.Ordinal))
                          .AddOrUpdate(player, false, (_, __) => false);

            return Task.CompletedTask;
        }

        public Task RemovePlayerAsync(string connectionId, string tableId)
        {
            if (_tables.TryGetValue(tableId, out var table))
            {
                var player = _connToPlayer.TryGetValue(connectionId, out var p) ? p : null;

                lock (table)
                {
                    if (player != null && table.Players.Remove(player))
                        table.PlayerCount = table.Players.Count;
                }

                if (player != null && _tableReadyMap.TryGetValue(tableId, out var readyMap))
                    readyMap.TryRemove(player, out _);
            }

            _connToTable.TryRemove(connectionId, out _);
            _connToPlayer.TryRemove(connectionId, out _);

            return Task.CompletedTask;
        }

        public (string tableId, string player)? DropConnection(string connectionId)
        {
            if (_connToTable.TryRemove(connectionId, out var tableId))
            {
                _connToPlayer.TryRemove(connectionId, out var player);

                if (tableId != null && _tables.TryGetValue(tableId, out var table) && player != null)
                {
                    lock (table)
                    {
                        table.Players.Remove(player);
                        table.PlayerCount = table.Players.Count;
                    }

                    if (_tableReadyMap.TryGetValue(tableId, out var readyMap))
                        readyMap.TryRemove(player, out _);
                }

                return (tableId, player ?? "");
            }

            return null;
        }

        public Task<string[]> GetPlayersAsync(string tableId)
        {
            if (_tables.TryGetValue(tableId, out var table))
                return Task.FromResult(table.Players.ToArray());

            return Task.FromResult(Array.Empty<string>());
        }

        // --------- READY ----------
        public Task SetReadyAsync(string tableId, string player, bool ready)
        {
            if (!_tables.ContainsKey(tableId))
                throw new InvalidOperationException("Table not found.");

            var map = _tableReadyMap.GetOrAdd(
                tableId,
                _ => new ConcurrentDictionary<string, bool>(StringComparer.Ordinal));

            map.AddOrUpdate(player, ready, (_, __) => ready);
            return Task.CompletedTask;
        }

        public Dictionary<string, bool>? GetReadyMap(string tableId)
        {
            if (_tableReadyMap.TryGetValue(tableId, out var map))
                return map.ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal);

            return null;
        }
    }
}