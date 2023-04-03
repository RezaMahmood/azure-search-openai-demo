namespace OpenAIDemoDotNet
{
    public interface IApproach
    {
        string Run(string question, Overrides overrides);
    }

}