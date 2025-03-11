using Business.Utilities.Results.Abstract;
using Entities.DTOs;
using Entities.Model;

namespace Business.Abstract
{
   public interface IAuthService
    {
        Task<IResult> RegisterAsync(RegisterDTO model);
        Task<IResult> UserEmailConfirm(string email, string otp);
        Task<IDataResult<Token>> LoginAsync(LoginDTO loginDTO);
        Task<IDataResult<string>> UpdateRefreshToken(string refreshToken, User appUser);
        Task<IDataResult<Token>> RefreshTokenLoginAsync(string refreshToken);
        Task<IResult> LogOut(string userId);


    }
}
