namespace myMessenger_back.Dtos
{
    public class UserDto    //todo add last name
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public DateTime CreatedOn { get; set; }
    }
}
