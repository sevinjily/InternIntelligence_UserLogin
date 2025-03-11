using Entities.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Utilities.Security.Abstract
{
   public interface ITokenService
    {
        Task<Token> CreateAccessToken(User appUser);
        string CreateRefreshToken();
    }
}
