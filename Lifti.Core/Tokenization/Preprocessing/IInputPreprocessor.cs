namespace Lifti.Tokenization.Preprocessing
{
    public interface IInputPreprocessor
    {
        PreprocessedInput Preprocess(char input);
    }
}
