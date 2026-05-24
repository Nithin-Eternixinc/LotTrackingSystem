using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace LTS.Common.Models;

public class User
{
    [Key]
    public int UserId { get; set; }
    public string UserName { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "Operator";

    public bool IsActive { get; set; } = true;

}
