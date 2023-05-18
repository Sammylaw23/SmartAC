namespace Theoremone.SmartAc.Application.Interfaces.Identity
{
    public interface ISmartAcJwtService
    {
        (string tokenId, string token) GenerateJwtFor(string targetId, string role);
    }
}