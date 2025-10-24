namespace GameServer.Contracts
{
    public class CreateTableReq
    {
        public string? Mode { get; set; }        // "classic101" | "tournament" | "katlamali"
        public int     Capacity { get; set; }    // 4 (default)
        public bool    IsPrivate { get; set; }
        public string? Password { get; set; }
        public int     EntryFee { get; set; }
    }
}