using Business.Abstract;
using Business.FluentValidation;
using Business.Utilities.Message.Abstract;
using Business.Utilities.Results.Abstract;
using Business.Utilities.Results.Concrete.ErrorResult;
using Business.Utilities.Results.Concrete.SuccessResult;
using Business.Utilities.Security.Abstract;
using Entities.DTOs;
using Entities.Model;
using Microsoft.AspNetCore.Identity;
using System.Net;

namespace Business.Concrete
{
    public class AuthManager : IAuthService
    {
        private readonly UserManager<User> _userManager;
        private readonly IMessageService _messageService;
        private readonly SignInManager<User> _signInManager;
        private readonly ITokenService _tokenService;


        public AuthManager(UserManager<User> userManager, IMessageService messageService, SignInManager<User> signInManager, ITokenService tokenService)
        {
            _userManager = userManager;
            _messageService = messageService;
            _signInManager = signInManager;
            _tokenService = tokenService;
        }

        private string GenerateOtp()
        {
            byte[] data = new byte[4];

            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            rng.GetBytes(data);
            int value = BitConverter.ToInt32(data, 0);
            return Math.Abs(value % 900000).ToString("D6");
        }
        public async Task<IResult> RegisterAsync(RegisterDTO model)
        {
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
                 return new ErrorResult("This email is already registered.", HttpStatusCode.BadRequest);

            var existingUserName = await _userManager.FindByNameAsync(model.UserName);
            if (existingUserName != null)
                return new ErrorResult("This username is already taken.", HttpStatusCode.BadRequest);

            User user = new()
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                UserName = model.UserName,
                OTP = GenerateOtp(),
                ExpiredDate = DateTime.UtcNow.AddMinutes(3),
                EmailConfirmed = false

            };


            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                try
                {
                    await _messageService.SendMessage(user.Email, "Welcome", user.OTP); 
                }
                catch (Exception ex)
                {
                    return new ErrorResult($"User created, but OTP sending failed: {ex.Message}", HttpStatusCode.InternalServerError);
                }
                return new SuccessResult("Successfully registered. Check your email for OTP.", HttpStatusCode.Created);
            }
            string response = string.Join(" ", result.Errors.Select(e => e.Description));
            return new ErrorResult(response, HttpStatusCode.BadRequest);
        }

      

        public async Task<IResult> UserEmailConfirm(string email, string otp)
        {
            var findUser = await _userManager.FindByEmailAsync(email);

            if (findUser == null)
                return new ErrorResult("User not found.", HttpStatusCode.NotFound);

            if (findUser.EmailConfirmed)
                return new ErrorResult("Email is already confirmed.", HttpStatusCode.BadRequest);


            if (findUser.LockoutEnd.HasValue && findUser.LockoutEnd > DateTime.UtcNow)
            {
                return new ErrorResult("Too many failed attempts. Try again later.", HttpStatusCode.Forbidden);
            }

            if (string.IsNullOrEmpty(findUser.OTP) || findUser.ExpiredDate <= DateTime.UtcNow)
            {
                return await ResendOtpAsync(email);
            }

            if (findUser.FailedAttempts >= 3) 
            {
                findUser.LockoutEnd = DateTime.UtcNow.AddMinutes(10);
                await _userManager.UpdateAsync(findUser);
                return new ErrorResult("Too many failed attempts. Your account is temporarily locked.", HttpStatusCode.Forbidden);
            }
            //Console.WriteLine($"Stored OTP: {findUser.OTP}, Entered OTP: {otp}, Expiry: {findUser.ExpiredDate}");

            if (findUser.OTP.Length < 6)
                return new ErrorResult("Invalid OTP format.", HttpStatusCode.BadRequest);


            if (findUser.OTP == otp && findUser.ExpiredDate > DateTime.UtcNow)
            {
                findUser.EmailConfirmed = true;
                findUser.OTP = null; 
                findUser.FailedAttempts = 0; 
                await _userManager.UpdateAsync(findUser);
                return new SuccessResult( "Email successfully confirmed.",HttpStatusCode.OK);
            }

            findUser.FailedAttempts++; 

            await _userManager.UpdateAsync(findUser);

            return new ErrorResult("Invalid OTP or expired.", HttpStatusCode.BadRequest);

        }
        public async Task<IResult> ResendOtpAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
                return new ErrorResult("User not found.", HttpStatusCode.NotFound);

            if (user.EmailConfirmed)
                return new ErrorResult("Email is already confirmed.", HttpStatusCode.BadRequest);

            user.OTP = GenerateOtp();
            user.ExpiredDate = DateTime.UtcNow.AddMinutes(3);
            await _userManager.UpdateAsync(user);

            try
            {
                await _messageService.SendMessage(user.Email, "New OTP Code", user.OTP);
            }
            catch (Exception ex)
            {
                return new ErrorResult($"OTP generation failed: {ex.Message}", HttpStatusCode.InternalServerError);
            }

            return new SuccessResult("New OTP has been sent to your email.", HttpStatusCode.OK);
        }

        public async Task<IDataResult<Token>> LoginAsync(LoginDTO loginDTO)
        {
            var validator = new LoginDTOValidation();
            var validationResult = await validator.ValidateAsync(loginDTO);

            if (!validationResult.IsValid)
            {
                // Validasiya uğursuz oldusa, səhv mesajlarını qaytar
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return new ErrorDataResult<Token>(message: string.Join(", ", errors), HttpStatusCode.BadRequest);
            }

            var findUser = await _userManager.FindByEmailAsync(loginDTO.UsernameOrEmail);
            if (findUser == null)
                findUser = await _userManager.FindByNameAsync(loginDTO.UsernameOrEmail);

            if (findUser == null)
                return new ErrorDataResult<Token>(message: "User does not exist!", HttpStatusCode.NotFound);

            if (findUser.EmailConfirmed == false)
            {
                return new ErrorDataResult<Token>(message: "User not confirmed", HttpStatusCode.BadRequest);
            }

            var result = await _signInManager.CheckPasswordSignInAsync(findUser, loginDTO.Password, false);
            var userRoles = await _userManager.GetRolesAsync(findUser);
            if (result.Succeeded)
            {
                Token token = await _tokenService.CreateAccessToken(findUser);
                var response = await UpdateRefreshToken(token.RefreshToken, findUser);
                return new SuccessDataResult<Token>(data: token, statusCode: HttpStatusCode.OK, message: response.Message);
            }
            else
            {
               
                return new ErrorDataResult<Token>(message: "Username or Password is not valid", HttpStatusCode.BadRequest);
            }


        }
        public async Task<IDataResult<string>> UpdateRefreshToken(string refreshToken, User appUser)
        {
            if (appUser is not null)
            {
                appUser.RefreshToken = refreshToken;
                appUser.RefreshTokenExpiredDate = DateTime.UtcNow.AddMonths(1);
                var result = await _userManager.UpdateAsync(appUser);
                if (result.Succeeded)
                {
                    return new SuccessDataResult<string>(data: refreshToken, HttpStatusCode.OK);

                }
                else
                {
                    string response = string.Empty;
                    foreach (var error in result.Errors)
                    {
                        response += error.Description + ".";
                    }
                    return new ErrorDataResult<string>(message: response, HttpStatusCode.BadRequest);
                }
            }
            else
            {
                return new ErrorDataResult<string>(HttpStatusCode.NotFound);
            }
        }
        public async Task<IDataResult<Token>> RefreshTokenLoginAsync(string refreshToken)
        {
            var user = _userManager.Users.FirstOrDefault(x => x.RefreshToken == refreshToken);
            //var userRoles = await _userManager.GetRolesAsync(user);

            if (user is not null && user.RefreshTokenExpiredDate > DateTime.Now)
            {
                Token token = await _tokenService.CreateAccessToken(user);
                token.RefreshToken = refreshToken;
                return new SuccessDataResult<Token>(data: token, statusCode: HttpStatusCode.OK);

            }
            else
            {
                return new ErrorDataResult<Token>(statusCode: HttpStatusCode.BadRequest);
            }
        }
        public async Task<IResult> LogOut(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is not null)
            {
                user.RefreshToken = null;
                user.RefreshTokenExpiredDate = null;
                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    return new SuccessResult(HttpStatusCode.OK);
                }
                else
                {
                    string response = string.Empty;
                    foreach (var error in result.Errors)
                    {
                        response += error.Description + ".";
                    }
                    return new ErrorResult(response, HttpStatusCode.BadRequest);
                }
            }

            return new ErrorResult(HttpStatusCode.NotFound);
        }

    }
}
