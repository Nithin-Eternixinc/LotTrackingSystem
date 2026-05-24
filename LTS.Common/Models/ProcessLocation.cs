using System;
using System.Collections.Generic;
using System.Text;
using LTS.Common.Constants;

namespace LTS.Common.Models;

public class ProcessLocation
{
    public int ProcessLocationId { get; set; }
    public string Name { get; set; } = "";
    public int SequenceNo { get; set; }
    public string Status { get; set; } = LocationStatus.Available;
    public bool IsEnabled { get; set; } = true;
    public int? CurrentLotId { get; set; }
    public Lot? CurrentLot { get; set; }


}
