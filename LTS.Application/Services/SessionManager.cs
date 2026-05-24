using System;
using System.Collections.Generic;
using System.Text;

using LTS.Common.Models;

namespace LTS.Application.Services;

public static class SessionManager
{
   public static User? CurrentUser { get; set; }

    public static bool IsEngineer
        => CurrentUser?.Role == "Engineer";

    public static bool IsOperator
       => CurrentUser?.Role == "Operator";

    public static bool IsLoggedIn
       => CurrentUser != null;

}
