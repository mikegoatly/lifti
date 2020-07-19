using System;
using System.Collections.Generic;
using System.Threading;

namespace Lifti.Tokenization.Preprocessing
{
    /// <inheritdoc />
    public class InputPreprocessorPipeline : ConfiguredBy<TokenizationOptions>, IInputPreprocessorPipeline
    {
        private readonly List<IInputPreprocessor> inputPreprocessors = new List<IInputPreprocessor>();
        private Queue<PreprocessedInput> processQueue = new Queue<PreprocessedInput>();
        private Queue<PreprocessedInput> outputQueue = new Queue<PreprocessedInput>();

        /// <inheritdoc />
        public IEnumerable<char> Process(char input)
        {
            if (this.inputPreprocessors.Count == 0)
            {
                yield return input;
                yield break;
            }

            this.processQueue.Enqueue(input);

            foreach (var preprocessor in this.inputPreprocessors)
            {
                while (this.processQueue.Count > 0)
                {
                    var toProcess = this.processQueue.Dequeue();
                    if (toProcess.Replacement != null)
                    {
                        foreach (var toProcessChar in toProcess.Replacement)
                        {
                            this.outputQueue.Enqueue(preprocessor.Preprocess(toProcessChar));
                        }
                    }
                    else
                    {
                        this.outputQueue.Enqueue(preprocessor.Preprocess(toProcess.Value));
                    }

                }

                this.outputQueue = Interlocked.Exchange(ref this.processQueue, this.outputQueue);
            }

            while (this.processQueue.Count > 0)
            {
                var toReturn = this.processQueue.Dequeue();
                if (toReturn.Replacement != null)
                {
                    foreach (var letter in toReturn.Replacement)
                    {
                        yield return letter;
                    }
                }
                else
                {
                    yield return toReturn.Value;
                }
            }
        }

        /// <inheritdoc />
        protected override void OnConfiguring(TokenizationOptions options)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (options.AccentInsensitive)
            {
                this.inputPreprocessors.Add(new LatinCharacterNormalizer());
            }

            if (options.CaseInsensitive)
            {
                this.inputPreprocessors.Add(new CaseInsensitiveNormalizer());
            }
        }
    }
}