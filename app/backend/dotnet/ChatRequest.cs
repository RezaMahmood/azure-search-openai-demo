using System.Collections.Generic;

namespace OpenAIDemoDotNet
{
    public class History
    {
        public string user { get; set; }
        public string bot { get; set; }
    }



    public class ChatRequest
    {
        public List<History> history { get; set; }
        public string approach { get; set; }
        public Overrides overrides { get; set; }
    }
}