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
using System.Xml.Linq;

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
            if (await db.UsersData.SingleOrDefaultAsync(item => item.Email == dataUserRegister.Email) != null)
            {
                throw new ApiException("Email already exists.");
            }
            CreatePasswordHash(dataUserRegister.Password, out byte[] passwordHash, out byte[] passwordSalt);
            UserData user = new UserData()
            {
                Email = dataUserRegister.Email,
                Name = dataUserRegister.Name,
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,
                CreatedOn = DateTime.UtcNow,
                Role = "user",
                RefreshToken = string.Empty,
                RefreshTokenCreatedOn = DateTime.UtcNow,
                RefreshTokenExpiresOn = DateTime.UtcNow,
            };

            db.UsersData.Add(user);             //Добавление записи в таблицу users_data
            await db.SaveChangesAsync();
            //string token = CreateToken(user);
            return new ApiResponse(message: "Successful registration."/*, result: token*/);
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
            var user = await db.UsersData.SingleOrDefaultAsync(item => item.Email == dataUserLogin.Email);
            if (user == null)
            {
                throw new ApiException("User not found.");
            }
            if (!VerifyPasswordHash(dataUserLogin.Password, user.PasswordHash, user.PasswordSalt))
            {
                throw new ApiException("Invalid password.");
            }
            string token = CreateToken(user);           // access-token

            var refreshToken = GenerateResreshToken();  // refresh-token

            SetRefreshToken(user, refreshToken);
            await db.SaveChangesAsync();
            return new ApiResponse(message: "Successful login.", result: new { token = token, refresh = refreshToken });
        }

        private RefreshToken GenerateResreshToken()
        {
            var refreshToken = new RefreshToken()
            {
                Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
                Expires = DateTime.UtcNow.AddDays(7), // AddDays(7)
                Created = DateTime.UtcNow
            };
            return refreshToken;
        }

        private void SetRefreshToken(UserData user, RefreshToken newRefreshToken)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = newRefreshToken.Expires
            };
            //Response.Cookies.Append("refreshToken", newRefreshToken.Token, cookieOptions);

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

        private string CreateToken(UserData user)
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
                expires: DateTime.UtcNow.AddDays(1), // AddDays(1)
                signingCredentials: creds);

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);

            return jwt;
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

        [Authorize]
        [HttpPost("refresh-token")]
        public async Task<ActionResult<string>> RefreshToken(string refreshToken)
        {
            var userJwt = GetCurrentUserEmailJwt();
            if (userJwt == null) throw new ApiException("Invalid token.");
            var user = await db.UsersData.SingleOrDefaultAsync(item => item.Email == userJwt.Email);
            //var refreshToken = Request.Cookies["refreshToken"];

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
            var data = await db.Users
                .Include(u => u.UserData)
                .Select(item => item.UserData.AsDto())
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
        public async Task<ActionResult<UserDataDto>> GetUser(int id)
        {
            var user = await db.Users.Include(u => u.UserData).SingleOrDefaultAsync(item => item.UserId == id);
            if (user is null)
            {
                return NotFound();
            }

            return user.UserData.AsDto();
        }

        [Authorize] //todo request for admin
        [HttpGet("get_all_conversations")]
        public async Task<ApiResponse> GetAllConversations()
        {
            var conversations = await db.Conversations
                .Include(creatorUserData => creatorUserData.CreatorUserData)
                .Include(senderUserData => senderUserData.LastMessageData.SenderUserData)
                .Include(lastMessageData => lastMessageData.LastMessageData)
                .Select(item => item.AsDto())
                .ToListAsync();
            return new ApiResponse("", conversations);
        }

        [Authorize]
        [HttpGet("get_my_info")]
        public async Task<ApiResponse> GetMyInfo()
        {
            var userJwt = GetCurrentUserEmailJwt();
            if (userJwt == null) throw new ApiException("Invalid token.");
            var user = await db.UsersData
                .SingleOrDefaultAsync(item => item.Email == userJwt.Email);
            if (user == null) throw new ApiException("User not found.");

            return new ApiResponse("", user.AsDto());
        }

        [Authorize]
        [HttpGet("get_my_conversations")]
        public async Task<ApiResponse> GetMyConversations()
        {
            var userJwt = GetCurrentUserEmailJwt();
            if (userJwt == null) throw new ApiException("Invalid token.");
            var user = await db.UsersData
                .SingleOrDefaultAsync(item => item.Email == userJwt.Email);
            if (user == null) throw new ApiException("User not found.");

            var conversations = await db.Conversations
                .Include(members => members.ConversationMembers)
                .Where(item => item.ConversationMembers.Any(element => element.UserData.Id == user.Id))
                .Select(item => new
                {
                    Id = item.Id,
                    Name = item.Name,
                    IsDirectMessage = item.IsDirectMessage,
                    CreatorUserId = item.CreatorUserId,
                    CreatorUserData = item.CreatorUserData.AsDto(),
                    LastMessageId = item.LastMessageId,
                    LastMessageData = item.LastMessageData.AsDto(),
                    ConversationMembers = item.ConversationMembers.Select(cm => new
                    {
                        Id = cm.Id,
                        UserId = cm.UserId,
                        UserData = cm.UserData.AsDto(),
                        ConversationId = cm.ConversationId,
                    }),
                }).ToListAsync();

            conversations = conversations.OrderByDescending(item => item.LastMessageData.SendingDate).ToList();

            return new ApiResponse("", conversations);
        }

        [Authorize] //todo request for admin
        [HttpGet("get_all_messages")]
        public async Task<ApiResponse> GetAllMessages()
        {
            var messages = await db.ConversationsMessages
                .Include(conversation => conversation.ConversationData)
                .Include(message => message.MessageData)
                    .ThenInclude(senderUserData => senderUserData.SenderUserData)
                .Select(item => new
                {
                    Id = item.Id,
                    ConversationId = item.ConversationId,
                    ConversationData = item.ConversationData.AsDto(),
                    MessageId = item.MessageId,
                    MessageData = item.MessageData.AsDto(),
                })
                .ToListAsync();
            return new ApiResponse("", messages);
        }
        [Authorize]
        [HttpGet("get_conversation_messages")]
        public async Task<ApiResponse> GetConversationMessages(int conversationId)
        {
            var userJwt = GetCurrentUserEmailJwt();
            if (userJwt == null) throw new ApiException("Invalid token.");
            var user = await db.UsersData
                .SingleOrDefaultAsync(item => item.Email == userJwt.Email);
            if (user == null) throw new ApiException("User not found.");
            var conversationMessages = await db.ConversationsMessages
                .Where(item => item.ConversationId == conversationId)
                .Include(conversation => conversation.ConversationData)
                .Include(message => message.MessageData)
                .Select(item => new
                {
                    Id = item.Id,
                    ConversationId = item.ConversationId,
                    ConversationData = item.ConversationData.AsDto(),
                    MessageId = item.MessageId,
                    MessageData = item.MessageData.AsDto(),
                })
                .ToListAsync();
            conversationMessages = conversationMessages.OrderBy(s => s.MessageData.SendingDate).ToList();
            var conversationMembers = await db.ConversationMembers
                .Where(item => item.ConversationId == conversationId)
                .Include(user => user.UserData)
                .Select(item => new
                {
                    Id = item.Id,
                    UserId = item.UserId,
                    UserData = item.UserData.AsDto(),
                })
                .ToListAsync();
            return new ApiResponse("", new { conversationMessages, conversationMembers });
        }

        [Authorize]
        [HttpPost("send_message")]
        public async Task<ActionResult<string>> SendMessage(UserSendMessage dataUserSendMessage)
        {
            var userJwt = GetCurrentUserEmailJwt();
            if (userJwt == null) throw new ApiException("Invalid token.");
            var user = await db.UsersData
                .SingleOrDefaultAsync(item => item.Email == userJwt.Email);
            if (user == null) throw new ApiException("User not found.");

            var conversation = db.Conversations.Where(item => item.Id == dataUserSendMessage.ConversationId).FirstOrDefault();  //Получение данных о беседе, в которой необходимо обновить данные
            if (conversation == null) throw new ApiException("Conversation not found.");

            var convMemb = db.ConversationMembers.Where(item => item.UserId == user.Id && item.ConversationId == dataUserSendMessage.ConversationId).FirstOrDefault();
            if (convMemb == null) throw new ApiException("Data error");
            
            Message message = new Message()
            {
                SenderId = user.Id,
                MessageText = dataUserSendMessage.MessageText,
                SendingDate = DateTime.UtcNow,
            };

            db.Messages.Add(message);   //Добавление записи в таблицу messages
            await db.SaveChangesAsync();

            ConversationsMessages conversationsMessages = new ConversationsMessages()
            {
                ConversationId = dataUserSendMessage.ConversationId,
                MessageId = message.Id,
            };

            db.ConversationsMessages.Add(conversationsMessages);    //Добавление записи в таблицу conversationsMessages
            await db.SaveChangesAsync();

            conversation.LastMessageId = message.Id;
            await db.SaveChangesAsync();

            return Ok();
        }
    }
}
