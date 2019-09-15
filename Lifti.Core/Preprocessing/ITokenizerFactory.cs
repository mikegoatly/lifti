namespace Lifti.Preprocessing
{
    public interface ITokenizerFactory
    {
        ITokenizer Create(TokenizationOptions options);
    }
}