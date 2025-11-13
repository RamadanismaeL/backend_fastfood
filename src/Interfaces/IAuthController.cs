using Microsoft.AspNetCore.Mvc;
using unipos_basic_backend.src.DTOs;

namespace unipos_basic_backend.src.Interfaces
{
    public interface IAuthController
    {
        Task<IActionResult> SignIn([FromBody] AuthRequestDTO authRequest);
        Task<IActionResult> RefreshToken();
        Task<IActionResult> SignOut();
        Task<IActionResult> CheckSession();
    }
}