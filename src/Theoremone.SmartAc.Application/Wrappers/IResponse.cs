namespace Theoremone.SmartAc.Application.Wrappers
{
    public interface IResponse
    {
        public bool Succeeded { get; set; }
        public List<string> Messages { get; set; }

    }
}
