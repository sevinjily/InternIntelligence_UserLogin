using Business.Results.Abstract;
using Entities.DTOs;

namespace Business.Abstract
{
   public interface IAuthService
    {
        Task<IResult> RegisterAsync(RegisterDTO model);
        Task<IResult> UserEmailConfirm(string email, string otp);

    }
}
