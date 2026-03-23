namespace AtlasWeb.Models
{
    public class Fatura : BaseEntity
    {
        // CS0108 düzeltildi: int Id kaldırıldı, BaseEntity'den kalıtılan Guid Id kullanılıyor
        public string FaturaNo { get; set; } = null!;
        public decimal Tutar { get; set; }
    }
}