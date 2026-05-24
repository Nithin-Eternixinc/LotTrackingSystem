using LTS.Common.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Text.RegularExpressions;

namespace LTS.Common.Interfaces;

public interface ILotRepository

{
    Task<IEnumerable<Lot>> GetAllAsync(); //getall lot
    Task<Lot?> GetByIdAsync(int lotId); //get lot by id
    //Task<IEnumerable<Lot>> GetByStatusAsync(string status); // get by status
    Task AddAsync(Lot lot); // add lot
    Task UpdateAsync(Lot lot);
    Task DeleteAsync(int lotId);

}


