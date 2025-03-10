using Business.Abstract;
using Business.Message.Abstract;
using Business.Results.Abstract;
using Business.Results.Concrete.ErrorResult;
using Business.Results.Concrete.SuccessResult;
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

        public AuthManager(UserManager<User> userManager, IMessageService messageService)
        {
            _userManager = userManager;
            _messageService = messageService;
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


            if (findUser.LockoutEnd.HasValue && findUser.LockoutEnd > DateTime.Now)
            {
                return new ErrorResult("Too many failed attempts. Try again later.", HttpStatusCode.Forbidden);
            }

            if (string.IsNullOrEmpty(findUser.OTP) || findUser.ExpiredDate <= DateTime.Now)
            {
                return new ErrorResult("OTP expired. Please request a new one.", HttpStatusCode.BadRequest);
            }

            if (findUser.FailedAttempts >= 3) 
            {
                findUser.LockoutEnd = DateTime.UtcNow.AddMinutes(10);
                await _userManager.UpdateAsync(findUser);
                return new ErrorResult("Too many failed attempts. Your account is temporarily locked.", HttpStatusCode.Forbidden);
            }

             if (findUser.OTP.Length < 6)
                return new ErrorResult("Invalid OTP format.", HttpStatusCode.BadRequest);


            if (findUser.OTP == otp && findUser.ExpiredDate > DateTime.Now)
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

    }
}
