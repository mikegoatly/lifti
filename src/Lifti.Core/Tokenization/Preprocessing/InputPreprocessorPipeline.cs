using System;
using System.Collections.Generic;
using System.Threading;

namespace Lifti.Tokenization.Preprocessing
{

    /// <inheritdoc />
    public class InputPreprocessorPipeline : IInputPreprocessorPipeline
    {
        private readonly List<IInputPreprocessor> inputPreprocessors = [];
        private static readonly SharedPool<Queue<PreprocessedInput>> queuePool = new(
            static () => new Queue<PreprocessedInput>(4),
            static q => q.Clear());

        /// <summary>
        /// Initializes a new instance of the <see cref="InputPreprocessorPipeline"/> class.
        /// </summary>
        public InputPreprocessorPipeline(TokenizationOptions options)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (options.IgnoreCharacters.Count > 0)
            {
                this.inputPreprocessors.Add(new IgnoredCharacterPreprocessor(options.IgnoreCharacters));
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

        /// <inheritdoc />
        public IEnumerable<char> Process(char input)
        {
            if (this.inputPreprocessors.Count == 0)
            {
                yield return input;
                yield break;
            }

            var processQueue = queuePool.Take();
            var outputQueue = queuePool.Take();

            processQueue.Enqueue(input);

            foreach (var preprocessor in this.inputPreprocessors)
            {
                while (processQueue.Count > 0)
                {
                    var toProcess = processQueue.Dequeue();
                    if (toProcess.Replacement != null)
                    {
                        foreach (var toProcessChar in toProcess.Replacement)
                        {
                            outputQueue.Enqueue(preprocessor.Preprocess(toProcessChar));
                        }
                    }
                    else
                    {
                        outputQueue.Enqueue(preprocessor.Preprocess(toProcess.Value));
                    }

                }

                outputQueue = Interlocked.Exchange(ref processQueue, outputQueue);
            }

            while (processQueue.Count > 0)
            {
                var toReturn = processQueue.Dequeue();
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

            queuePool.Return(processQueue);
            queuePool.Return(outputQueue);
        }
    }
}