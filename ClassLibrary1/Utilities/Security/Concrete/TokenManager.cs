using Business.Utilities.Security.Abstract;
using Entities.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Business.Utilities.Security.Concrete
{
  public  class TokenManager:ITokenService
    {
        private readonly IConfiguration _configuration;
        private readonly UserManager<User> _userManager;

        public TokenManager(IConfiguration configuration, UserManager<User> userManager)
        {
            _configuration = configuration;
            _userManager = userManager;
        }

        public async Task<Token> CreateAccessToken(User appUser)
        {
            var securityKey = _configuration["Token:SecurityKey"];
            if (string.IsNullOrEmpty(securityKey))
            {
                throw new Exception("Security key is null or empty! Check your appsettings.json.");
            }
            Token token = new();
            var claims = new List<Claim>()
            {
                new Claim(JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier,appUser.Id.ToString()),

            };
            //foreach (var role in roles)
            //{
            //    claims.Add(new Claim(ClaimTypes.Role, role));
            //}
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Token:SecurityKey"]));
            token.ExpiredDate = DateTime.UtcNow.AddMinutes(2);
            JwtSecurityToken securityToken = new(

                audience: _configuration["Token:Audience"],
                issuer: _configuration["Token:Issuer"],
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: token.ExpiredDate,
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));


            JwtSecurityTokenHandler tokenHandler = new();
            token.AccessToken = tokenHandler.WriteToken(securityToken);
            token.RefreshToken = CreateRefreshToken();
            await _userManager.AddClaimsAsync(appUser, claims);
            return token;
        }

        public string CreateRefreshToken()
        {
            byte[] number = new byte[32];
            using RandomNumberGenerator random = RandomNumberGenerator.Create();
            random.GetBytes(number);
            return Convert.ToBase64String(number);
        }
    }
}
