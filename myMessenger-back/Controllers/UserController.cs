using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using myMessenger_back.Dtos;
using myMessenger_back.Models;

namespace myMessenger_back.Controllers
{
    [Route("users_info")]
    public class UserController: ControllerBase
    {
        private ApplicationContext db;
        private IConfiguration _config;
        public UserController(ApplicationContext context, IConfiguration configuration)
        {
            db = context;
            _config = configuration;
        }

        /// <summary>
        /// Получить данные пользователя
        /// </summary>
        /// /// <param name="userId"></param>
        /// <returns></returns>
        [HttpGet("{userId}")]
        [Authorize]
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
