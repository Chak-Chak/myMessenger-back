namespace myMessenger_back.Dtos
{
    public class UserDataDto    //todo add last name
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public DateTime CreatedOn { get; set; }
    }
}
