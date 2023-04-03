namespace OpenAIDemoDotNet
{


    public class Overrides
    {
        public bool? semantic_ranker { get; set; }
        public bool? semantic_captions { get; set; }
        public int? top { get; set; }
        public bool? suggest_followup_questions { get; set; }

        public string exclude_category { get; set; }
        public string prompt_template { get; set; }
        public float? temperature { get; set; }
    }
}