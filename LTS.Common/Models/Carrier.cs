using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace LTS.Common.Models;

public class Carrier 
{
    [Key]
    public int CarrierId { get; set; }
    public string CarrierCode { get; set; } = "";
    public int Capacity { get; set; } = 25;
    public string Status { get; set; } = "Unallocated";
    public string CurrentLocation { get; set; } = "Pl-01";
    public DateTime CreatedOn { get; set; } = DateTime.Now;

}
