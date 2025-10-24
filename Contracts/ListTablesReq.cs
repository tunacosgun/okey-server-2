namespace GameServer.Contracts
{
    /// <summary>Masaları listeleme isteği.</summary>
    public class ListTablesReq
    {
        /// <summary>İsteğe bağlı mod filtresi (örn: "tournament", "classic101").</summary>
        public string? Mode { get; set; }

        /// <summary>Yalnızca aktif (IsActive=true) masaları getir.</summary>
        public bool OnlyActive { get; set; } = true;

        /// <summary>Dönüşte maksimum kayıt sayısı (opsiyonel).</summary>
        public int Limit { get; set; } = 50;
    }
}