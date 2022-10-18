using AutoWrapper.Wrappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using myMessenger_back.Dtos;
using myMessenger_back.Models;
using myMessenger_back.Models.Additional;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
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


        /*[AllowAnonymous]
        [HttpPost("authenticate")]
        public IActionResult Authenticate(AuthenticateRequest model)
        {
            var response = _userService.Authenticate(model, ipAddress());
            setTokenCookie(response.RefreshToken);
            return Ok(response);
        }

        [AllowAnonymous]
        [HttpPost("refresh-token")]
        public IActionResult RefreshToken()
        {
            var refreshToken = Request.Cookies["refreshToken"];
            var response = _userService.RefreshToken(refreshToken, ipAddress());
            setTokenCookie(response.RefreshToken);
            return Ok(response);
        }

        [HttpPost("revoke-token")]
        public IActionResult RevokeToken(RevokeTokenRequest model)
        {
            // accept refresh token in request body or cookie
            var token = model.Token ?? Request.Cookies["refreshToken"];

            if (string.IsNullOrEmpty(token))
                return BadRequest(new { message = "Token is required" });

            _userService.RevokeToken(token, ipAddress());
            return Ok(new { message = "Token revoked" });
        }

        [HttpGet("{id}/refresh-tokens")]
        public IActionResult GetRefreshTokens(int id)
        {
            var user = _userService.GetById(id);
            return Ok(user.RefreshTokens);
        }

        private void setTokenCookie(string token)
        {
            // append cookie with refresh token to the http response
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.UtcNow.AddDays(7)
            };
            Response.Cookies.Append("refreshToken", token, cookieOptions);
        }

        private string ipAddress()
        {
            // get source ip address for the current request
            if (Request.Headers.ContainsKey("X-Forwarded-For"))
                return Request.Headers["X-Forwarded-For"];
            else
                return HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();
        }*/

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
                CreatedOn = DateTime.UtcNow
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();
            //todo generate tokens
            return new ApiResponse(message: "Successful registration.", result: "");
        }

        private void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            using (var hmac = new HMACSHA512())
            {
                passwordSalt = hmac.Key;
                passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
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
            return new ApiResponse(message: "Successful login.", result: token);
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
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config.GetSection("Secret:Key").Value));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: creds);

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);

            return jwt;
        }

    /// <summary>
    /// Получить данные о всех пользователях
    /// </summary>
    /// <returns></returns>
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
        /// /// <param name="userId"></param>
        /// <returns></returns>
        [HttpGet("{id}")]
        //[Authorize]
        public async Task<ActionResult<UserDto>> GetUser(int userId)
        {
            var user = await db.Users.SingleOrDefaultAsync(item => item.Id == userId);
            if (user is null)
            {
                return NotFound();
            }

            return user.AsDto();
        }
    }
}
