using System;
using System.Collections.Generic;
using System.Threading;

namespace Lifti
{
    public class InputPreprocessorPipeline : IInputPreprocessorPipeline
    {
        private readonly IInputPreprocessor[] inputPreprocessors;

        public InputPreprocessorPipeline(IInputPreprocessor[] inputPreprocessor)
        {
            this.inputPreprocessors = inputPreprocessor ?? Array.Empty<IInputPreprocessor>();
        }

        /// <remarks>
        /// Processed input is not expected to contain any word break characters, e.g.
        /// whitespace or punctuation.
        /// </remarks>
        public IEnumerable<char> Process(char input)
        {
            if (this.inputPreprocessors.Length == 0)
            {
                yield return input;
                yield break;
            }

            var processQueue = new Queue<PreprocessedInput>(); // Pool?
            processQueue.Enqueue(input);
            var outputQueue = new Queue<PreprocessedInput>(); // Pool?

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

            foreach (var toReturn in processQueue)
            {
                // Duplicated as above - could be reduced to shared logic in PreprocessedInput?
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
    }
}
