using Microsoft.AspNetCore.Mvc;
using unipos_basic_backend.src.DTOs;

namespace unipos_basic_backend.src.Interfaces
{
    public interface ICashRegisterController
    {
        Task<ActionResult<IEnumerable<CashRegisterListDTO>>> GetAllAsync();
        Task<IActionResult> OpenRegisterAsync([FromBody] CashRegisterOpenDTO cashRegister);
        Task<IActionResult> CloseRegisterAsync([FromBody] CashRegisterCloseDTO cashRegister);
        Task<IActionResult> DeleteAsync([FromRoute] Guid id);
        Task<ActionResult<IEnumerable<CashRegisterSelectUserDTO>>> GetSelectUserToOpenCash();
        Task<ActionResult<IEnumerable<CashRegisterSelectUserDTO>>> GetSelectUserToCloseCash();
        Task<ActionResult<IEnumerable<CashRegisterDetailListDTO>>> GetAllDetails([FromRoute] Guid cashRegisterId);
        Task<IActionResult> CreateCashDetails([FromBody] CashRegisterDetailCreateDTO cashRegister);
        Task<IActionResult> UpdateCashDetails([FromBody] CashRegisterDetailUpdateDTO cashRegister);
        Task<ActionResult<CashRegisterCardsDTO>> GetCardAsync();
        Task<ActionResult<CashRegisterCardsDTO>> GetCardAsync([FromRoute] Guid id);
    }
}