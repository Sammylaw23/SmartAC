namespace Theoremone.SmartAc.Application.Wrappers
{
    public class Response<T> : IResponse
    {
        public Response()
        {

        }
        public bool Succeeded { get; set; }
        public List<string> Messages { get; set; } = new List<string>();
        public T? Data { get; set; }
    }
}
