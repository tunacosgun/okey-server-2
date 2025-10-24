namespace GameServer.Contracts
{
    public class SetReadyReq
    {
        public string TableId { get; set; } = "";
        public string Player  { get; set; } = "";
        public bool   Ready   { get; set; } = false;
    }
}