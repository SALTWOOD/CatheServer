using Microsoft.IdentityModel.Tokens;
using SQLite;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CatheServer.Modules.Database
{
    public class UserEntity
    {
        [AutoIncrement, PrimaryKey]
        [Column("id")]
        public int Id { get; set; }

        [Indexed]
        [Column("username")]
        public string UserName { get; set; } = string.Empty;

        [Column("password")]
        public byte[] Password { get; set; } = new byte[64];

        [Indexed]
        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [Column("uuid")]
        public string Uuid { get; set; } = Guid.NewGuid().ToString();

        public static UserEntity CreateEntity()
        {
            UserEntity clusterEntity = new UserEntity();
            return clusterEntity;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, Uuid);
        }

        public override string ToString()
        {
            return $"<{this.GetType().FullName} instance index={this.Id} user={this.UserName} code={this.Uuid}>";
        }

        public string GenerateJwtToken(RSA rsa)
        {
            // 设置JWT的密钥
            var securityKey = new RsaSecurityKey(rsa);

            // 创建JWT的签名凭证
            var signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha384);

            // 设置JWT的Claims
            var claims = new[]
            {
                new Claim(ClaimTypes.Email, this.Email)
            };

            // 创建JWT的Token
            var token = new JwtSecurityToken(
               issuer: "cathe_server",
               audience: this.UserName,
               claims: claims,
               expires: DateTime.Now.AddDays(30),
               signingCredentials: signingCredentials
            );

            // 生成JWT字符串
            var jwtToken = new JwtSecurityTokenHandler().WriteToken(token);
            return jwtToken;
        }

        public static bool VerifyJwtToken(RSA rsa, string jwtToken, string username, [MaybeNullWhen(true)] out Exception ex)
        {
            // 设置JWT的密钥
            var securityKey = new RsaSecurityKey(rsa);

            // 验证JWT的密钥
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = securityKey,
                ValidIssuer = "cathe_server",
                ValidAudience = username
            };

            try
            {
                // 验证JWT字符串
                var claimsPrincipal = new JwtSecurityTokenHandler().ValidateToken(jwtToken, tokenValidationParameters, out _);
                ex = null;
                return true;
            }
            catch (Exception e)
            {
                ex = e;
                return false;
            }
        }
    }
}
