using AutoWrapper.Wrappers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using myMessenger_back.Dtos;
using myMessenger_back.Models;

namespace myMessenger_back.Controllers
{
    [ApiController]
    [Route("users")]
    public class UsersController : ControllerBase
    {
        private ApplicationContext db;
        private IConfiguration _config;
        public UsersController(ApplicationContext context, IConfiguration configuration)
        {
            db = context;
            _config = configuration;
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
