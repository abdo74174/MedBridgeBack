using MedBridge.Dtos;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace MedBridge.Services
{
    public interface IContactUsService
    {
        Task<IActionResult> GetAsync();
        Task<IActionResult> AddAsync(ContactUsDto contactUs);
    }
}