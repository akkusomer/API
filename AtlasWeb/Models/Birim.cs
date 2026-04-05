namespace AtlasWeb.Models
{
    public class Birim : BaseEntity
    {
        public string Ad { get; set; } = string.Empty;
        public string Sembol { get; set; } = string.Empty;

        public virtual ICollection<Stok> Stoklar { get; set; } = new List<Stok>();
    }
}
