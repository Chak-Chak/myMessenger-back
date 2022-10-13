using myMessenger_back.Dtos;
using myMessenger_back.Models;

namespace myMessenger_back
{
    public static class Extensions
    {
        public static UserDto AsDto(this User user)
        {
            return new UserDto()
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                CreatedOn = user.CreatedOn
            };
        }
    }
}
