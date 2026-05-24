using System;
using System.Collections.Generic;
using System.Text;

namespace LTS.Common.Models;
public class LotRoute
{
    public int LotRouteId { get; set; }
    public int LotId { get; set; }
    public int ProcessLocationId { get; set; }
    public int StepOrder { get; set; }           
    public bool IsCompleted { get; set; } = false;

    public Lot? Lot { get; set; }
    public ProcessLocation? ProcessLocation { get; set; }
}


