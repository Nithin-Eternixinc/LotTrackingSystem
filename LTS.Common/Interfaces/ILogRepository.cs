using LTS.Common.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace LTS.Common.Interfaces;

public interface ILogRepository
{
    Task AddAsync(ApplicationLog log); //every action in the system writes a log entry to DB
    Task<IEnumerable<ApplicationLog>> GetAllAsync();  //get every log

}
