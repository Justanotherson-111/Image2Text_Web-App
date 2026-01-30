namespace backend.Services.Interfaces;

public interface ICorrector
{
    Task<string> CorrectAsync(string text, string language);
}
