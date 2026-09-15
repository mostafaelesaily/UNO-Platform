using System;
using System.Collections.Generic;
using System.Text;
using UNO.Application.DTOs.Common.identity;
using UNO.Domain.Entities.Common.Identity;

namespace UNO.Application.Interfaces.Common
{
    public interface IAccountService
    {
        public Task<string> GenrateAccessToken(AppUser appUser);
        public Task<RefreshTokens> GenrateRefreshToken();
        public Task<AuthenticationResponseDto> HandleRefreshTokenAsync(GenrateRefreshToken refreshTokenDto);
    }
}
