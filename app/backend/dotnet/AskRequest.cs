namespace OpenAIDemoDotNet
{

    public class AskRequest
    {
        public string question { get; set; }
        public string approach { get; set; }
        public Overrides overrides { get; set; }
    }
}