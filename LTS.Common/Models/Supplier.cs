using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace LTS.Common.Models;

public class Supplier
{
    [Key]
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = "";
    public string ContactName { get; set; } = "";
    public string ContactEmail { get; set; } = "";
    public DateTime CreatedOn { get; set; } = DateTime.Now;
    public bool IsActive { get; set; } = true;

    
}
