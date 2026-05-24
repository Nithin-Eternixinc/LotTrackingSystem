using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace LTS.Common.Models;
public class Lot
{
    [Key]
    public int LotId { get; set; }
    public string LotName { get; set; } = "";
    public int WaferCount { get; set; }
    public string LotStatus { get; set; } = "Idle";
    public int? CurrentProcessLocationId { get; set; }
    public ProcessLocation? CurrentProcessLocation { get; set; }
    public DateTime CreatedOn { get; set; } = DateTime.Now;
    public DateTime? StartedOn { get; set; }
    public DateTime? CompletedOn { get; set; }
    public int CarrierId { get; set; }
    public Carrier? Carrier { get; set; }
    public ICollection<LotRoute> LotRoutes { get; set; } = new List<LotRoute>();


}
