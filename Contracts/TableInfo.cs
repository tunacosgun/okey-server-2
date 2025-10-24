using System.Collections.Generic;

namespace GameServer.Contracts
{
    public class TableInfo
    {
        public string Id { get; set; } = "";
        public string Mode { get; set; } = "classic101";
        public int Capacity { get; set; } = 4;
        public int PlayerCount { get; set; } = 0;
        public List<string> Players { get; set; } = new();
        public bool IsPrivate { get; set; }
        public string? Password { get; set; }
        public int EntryFee { get; set; } = 0;
        public bool IsActive { get; set; } = true;
    }
}