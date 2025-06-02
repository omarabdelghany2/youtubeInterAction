namespace SignalRGame.Models;

public class MyUser
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string PasswordHash { get; set; }  // store hashed passwords!
    public string interaction{get ; set; }= "";
    public string platform {get;set;}= "";
}
