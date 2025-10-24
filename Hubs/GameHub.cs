using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameServer.Contracts;
using GameServer.Services;
using Microsoft.AspNetCore.SignalR;

namespace GameServer.Hubs
{
    public class GameHub : Hub
    {
        private readonly TableService _tables;

        public GameHub(TableService tables) => _tables = tables;

        // ---------- CREATE ----------
        // Tek resmi isim: CreateTable
        public Task<string> CreateTable(CreateTableReq req) =>
            _tables.CreateTableAsync(req);

        // Alternatif basit varyant (farklı isim!)
        public Task<string> CreateTable2(string mode, int capacity) =>
            _tables.CreateTableAsync(new CreateTableReq { Mode = mode, Capacity = capacity });

        // Quick create
        public Task<string> CreateTableSimple() =>
            _tables.CreateTableSimpleAsync();

        // ---------- JOIN / LEAVE ----------
        public async Task JoinTable(string tableId, string player)
        {
            await _tables.AddPlayerAsync(Context.ConnectionId, tableId, player);
            await Groups.AddToGroupAsync(Context.ConnectionId, tableId);

            // İstemcide dinlenen event
            await Clients.Caller.SendAsync("JoinTableOk", tableId);
            await PushPlayers(tableId);

            // Eğer tabloda hazır durumu tutuluyorsa, son durumu da it
            var readyMap = _tables.GetReadyMap(tableId);
            if (readyMap is not null)
                await Clients.Group(tableId).SendAsync("ReadyChanged", tableId, readyMap);
        }

        public async Task LeaveTable(string tableId)
        {
            await _tables.RemovePlayerAsync(Context.ConnectionId, tableId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, tableId);

            await PushPlayers(tableId);

            var readyMap = _tables.GetReadyMap(tableId);
            if (readyMap is not null)
                await Clients.Group(tableId).SendAsync("ReadyChanged", tableId, readyMap);
        }

        // ---------- CHAT ----------
        public Task SendMessage(string tableId, string user, string message) =>
            Clients.Group(tableId).SendAsync("ReceiveMessage", user, message);

        // ---------- LISTS / QUERIES ----------
        public Task<List<TableInfo>> ListTables(ListTablesReq req) =>
            _tables.ListTablesAsync(req);

        public Task<string[]> GetPlayers(string tableId) =>
            _tables.GetPlayersAsync(tableId);

        // ---------- READY ----------
        // *** İMZA: Unity istemciyle birebir aynı ***
        public async Task SetReady(string tableId, string player, bool ready)
        {
            await _tables.SetReadyAsync(tableId, player, ready);

            // Bilgilendir
            await Clients.Caller.SendAsync("AckReady", tableId);

            var map = _tables.GetReadyMap(tableId);
            if (map is not null)
                await Clients.Group(tableId).SendAsync("ReadyChanged", tableId, map);
        }

        // ---------- CONNECTION LIFECYCLE ----------
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var info = _tables.DropConnection(Context.ConnectionId);
            if (info is { } x)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, x.tableId);
                await PushPlayers(x.tableId);

                var map = _tables.GetReadyMap(x.tableId);
                if (map is not null)
                    await Clients.Group(x.tableId).SendAsync("ReadyChanged", x.tableId, map);
            }

            await base.OnDisconnectedAsync(exception);
        }

        // ---------- HELPERS ----------
        private async Task PushPlayers(string tableId)
        {
            var players = await _tables.GetPlayersAsync(tableId);
            // İstemci: conn.On<string, string[]>("PlayersChanged", ...)
            await Clients.Group(tableId).SendAsync("PlayersChanged", tableId, players);
        }
    }
}