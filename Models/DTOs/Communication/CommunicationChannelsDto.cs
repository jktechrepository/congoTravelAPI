namespace CongoTravel.Models.DTOs.Communication
{
    public class CommunicationChannelsDto
    {
        public bool Push { get; set; } = true;
        public bool Email { get; set; } = false;
        public bool Sms { get; set; } = false;
        public bool InApp { get; set; } = true;
    }
}

