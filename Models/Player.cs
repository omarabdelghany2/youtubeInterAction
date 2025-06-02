namespace SignalRGame.Models
{
    public class Player
    {
        public string userId { get; set; } = string.Empty;
        public string profileName { get ;set; }= string.Empty;
        public string team { get; set; } = "Unassigned"; // Team can be "Blue" or "Red"
        public int score{ get; set;}= 0;
        public int gameScore{get;set;}=0;
        public int profileScore{get;set;}=0;
        public bool answered{get;set;}=false;
        public bool inGame{get;set;}=false;
        
        public bool answereCorrectModeTwo=false;
    }
}
