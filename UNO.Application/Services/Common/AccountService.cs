using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using UNO.Application.DTOs.Common.identity;
using UNO.Application.Exceptions;
using UNO.Application.Interfaces.Common;
using UNO.Domain.Entities.Common.Identity;

namespace UNO.Application.Services.Common
{
    public class AccountService : IAccountService
    {
        public AccountService
            (
             UserManager<AppUser> userManager,
             ILogger<AccountService> logger,
             IConfiguration configuration,
             IMapper mapper
            )
        {
            this.mapper = mapper;
            this.userManager = userManager;
            this.logger = logger;
            this.configuration = configuration;
        }
        public UserManager<AppUser> userManager;
        public ILogger<AccountService> logger;
        public IConfiguration configuration;
        public IMapper mapper;
        public async Task<string> GenrateAccessToken(AppUser appUser)
        {
            var claims = new List<Claim>()
            {
             new Claim(ClaimTypes.NameIdentifier, appUser.Id),
             new Claim(ClaimTypes.Email, appUser.Email),
             new Claim(ClaimTypes.Name, appUser.UserName),
             new Claim(JwtRegisteredClaimNames.Jti , Guid.NewGuid().ToString())
            };
            var roles = await userManager.GetRolesAsync(appUser);
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:Key"]));
            var sigKey = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken
            (
                issuer: configuration["JWT:Issuer"],
                audience: configuration["JWT:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(3),
                signingCredentials: sigKey
            );
            var _token = new JwtSecurityTokenHandler().WriteToken(token);
            return _token;
        }

        public async Task<RefreshTokens> GenrateRefreshToken()
        {
            var RefreshToken = new RefreshTokens()
            {
                CreatedOn = DateTime.UtcNow,
                ExpiresOn = DateTime.UtcNow.AddDays(7),
                Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
            };
            return RefreshToken;
        }

        public async Task<AuthenticationResponseDto> HandleRefreshTokenAsync(GenrateRefreshToken refreshTokenDto)
        {
            if (refreshTokenDto == null) throw new ArgumentNullException(nameof(refreshTokenDto));
            var user = userManager.Users.FirstOrDefault(u => u.RefreshTokens.Any(t => t.Token == refreshTokenDto.Token));
            if (user == null) throw new UnauthorizedException("Invalid Refresh Token");

            var refreshToken = user.RefreshTokens.FirstOrDefault(t => t.Token == refreshTokenDto.Token);
            if (refreshToken == null || refreshToken.ExpiresOn < DateTime.UtcNow || refreshToken.revokedOn != null)
            {
                throw new UnauthorizedException("Invalid or Expired Refresh Token");
            }
            refreshToken.revokedOn = DateTime.UtcNow;
            var newAccessToken = await GenrateAccessToken(user);
            var newRefreshToken = await GenrateRefreshToken();
            user.RefreshTokens.Add(newRefreshToken);
            await userManager.UpdateAsync(user);
            return new AuthenticationResponseDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken.Token,
                ExpireOn = DateTime.UtcNow.AddHours(3),
                message = "Authentication Completed Successfulley !"
            };
        }
    }
}
