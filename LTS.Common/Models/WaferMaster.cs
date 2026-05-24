using System.ComponentModel.DataAnnotations;

namespace LTS.Common.Models;

public class WaferMaster
{
    [Key]
    public int WaferId { get; set; }
    public string WaferSerialNo { get; set; } = "";
    public int SupplierId { get; set; }
    public int? LotId { get; set; }
    public string WaferStatus { get; set; } = "Unallocated";
    public DateTime CreatedOn { get; set; } = DateTime.Now;

    public Supplier? Supplier { get; set; }
    public Lot? Lot { get; set; }


}