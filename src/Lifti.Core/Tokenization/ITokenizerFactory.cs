namespace Lifti.Tokenization
{
    public interface ITokenizerFactory
    {
        ITokenizer Create(TokenizationOptions options);
    }
}