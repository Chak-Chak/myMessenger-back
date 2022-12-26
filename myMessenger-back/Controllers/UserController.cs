using AutoWrapper.Wrappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using myMessenger_back.Dtos;
using myMessenger_back.Models;
using myMessenger_back.Models.Additional;
using Newtonsoft.Json.Linq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace myMessenger_back.Controllers
{
    [ApiController]
    [Route("users")]
    public class UsersController : ControllerBase
    {
        private ApplicationContext db;
        private readonly IConfiguration _config;
        public UsersController(ApplicationContext context, IConfiguration configuration)
        {
            db = context;
            _config = configuration;
        }

        [AllowAnonymous]
        [HttpGet("ping")]
        public async Task<ApiResponse>
            Ping()
        {
            return new ApiResponse(message: "pong");
        }

        /// <summary>
        /// Регистрация
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<ApiResponse>
            Register(UserRegister dataUserRegister)
        {
            if (await db.Users.SingleOrDefaultAsync(item => item.Email == dataUserRegister.Email) != null)
            {
                throw new ApiException("Email already exists.");
            }
            CreatePasswordHash(dataUserRegister.Password, out byte[] passwordHash, out byte[] passwordSalt);
            User user = new User()
            {
                Email = dataUserRegister.Email,
                Name = dataUserRegister.Name,
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,
                CreatedOn = DateTime.UtcNow,
                Role = "user",
                RefreshToken = string.Empty,
                RefreshTokenCreatedOn = DateTime.MinValue,
                RefreshTokenExpiresOn = DateTime.MinValue,
            };
            
            db.Users.Add(user);             //Добавление записив таблицу users
            await db.SaveChangesAsync();
            string token = CreateToken(user);
            return new ApiResponse(message: "Successful registration.", result: token);
        }

        private void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            using (var hmac = new HMACSHA512())
            {
                passwordSalt = hmac.Key;
                passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            }
        }

        /// <summary>
        /// Авторизация
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<ApiResponse> Login(UserLogin dataUserLogin)
        {
            var user = await db.Users.SingleOrDefaultAsync(item => item.Email == dataUserLogin.Email);
            if (user == null)
            {
                throw new ApiException("User not found.");
            }
            if (!VerifyPasswordHash(dataUserLogin.Password, user.PasswordHash, user.PasswordSalt))
            {
                throw new ApiException("Invalid password.");
            }
            string token = CreateToken(user);

            var refreshToken = GenerateResreshToken();
            SetRefreshToken(user, refreshToken);
            await db.SaveChangesAsync();
            return new ApiResponse(message: "Successful login.", result: token);
        }

        private RefreshToken GenerateResreshToken()
        {
            var refreshToken = new RefreshToken()
            {
                Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
                Expires = DateTime.UtcNow.AddDays(7),
                Created = DateTime.UtcNow
            };
            return refreshToken;
        }

        private void SetRefreshToken(User user, RefreshToken newRefreshToken)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = newRefreshToken.Expires
            };
            Response.Cookies.Append("refreshToken", newRefreshToken.Token, cookieOptions); //todo edit for android

            user.RefreshToken = newRefreshToken.Token;
            user.RefreshTokenCreatedOn = newRefreshToken.Created;
            user.RefreshTokenExpiresOn = newRefreshToken.Expires;
        }

        private bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt)
        {
            using (var hmac = new HMACSHA512(passwordSalt))
            {
                var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                return computedHash.SequenceEqual(passwordHash);
            }
        }

        private string CreateToken(User user)
        {
            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config.GetSection("Jwt:Key").Value));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddDays(1),
                signingCredentials: creds);

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);

            return jwt;
        }

        [Authorize]
        [HttpPost("refresh-token")]
        public async Task<ActionResult<string>> RefreshToken()
        {
            var userJwt = GetCurrentUserEmailJwt();
            if (userJwt == null) throw new ApiException("Invalid token.");
            var user = await db.Users.SingleOrDefaultAsync(item => item.Email == userJwt.Email);
            var refreshToken = Request.Cookies["refreshToken"];

            if (user == null) throw new ApiException("User not found.");

            if (!user.RefreshToken.Equals(refreshToken))
            {
                return Unauthorized("Invalid refresh token");
            } 
            else if (user.RefreshTokenExpiresOn < DateTime.UtcNow)
            {
                return Unauthorized("Token expired");
            }

            string token = CreateToken(user);
            var newRefreshToken = GenerateResreshToken();
            SetRefreshToken(user, newRefreshToken);

            await db.SaveChangesAsync();
            return Ok(token);
        }

        /// <summary>
        /// Получить данные о всех пользователях
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("all")]
        public async Task<ApiResponse>
            GetUsers()
        {
            var data = await db.Users.Select(item => item.AsDto())
                .ToListAsync();
            return new ApiResponse("", data);
        }

        /// <summary>
        /// Получить данные пользователя
        /// </summary>
        /// /// <param name="id"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("{id}")]
        public async Task<ActionResult<UserDto>> GetUser(int id)
        {
            var user = await db.Users.SingleOrDefaultAsync(item => item.Id == id);
            if (user is null)
            {
                return NotFound();
            }

            return user.AsDto();
        }

        private UserJwt? GetCurrentUserEmailJwt()
        {
            if (HttpContext.User.Identity is not ClaimsIdentity identity) return null;
            var userClaims = identity.Claims;
            return new UserJwt()
            {
                Email = userClaims.SingleOrDefault(x => x.Type == ClaimTypes.Email)?.Value ?? string.Empty
            };
        }
    }
}
