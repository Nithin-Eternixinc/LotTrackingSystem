using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace LTS.Common.Models;

public class ApplicationLog
{
    [Key]
    public int LogId { get; set; }
    public string Level { get; set; } = "INFO";
    public string Message { get; set; } = "";
    public DateTime Timestamp { get; set; } = DateTime.Now;
}