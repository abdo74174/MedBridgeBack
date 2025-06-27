using MedBridge.Dtos.ShippingPriceDto;
using MedicalStoreAPI.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MedBridge.Services
{
    public interface IShippingPriceService
    {
        Task<IActionResult> GetShippingPrices();
        Task<IActionResult> UpdateShippingPrice(ShippingPriceUpdateDto dto);
    }
}