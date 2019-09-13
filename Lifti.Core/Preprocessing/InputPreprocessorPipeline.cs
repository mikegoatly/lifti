using System;
using System.Collections.Generic;
using System.Threading;

namespace Lifti.Preprocessing
{
    public class InputPreprocessorPipeline : IInputPreprocessorPipeline
    {
        private readonly IInputPreprocessor[] inputPreprocessors;
        private Queue<PreprocessedInput> processQueue = new Queue<PreprocessedInput>();
        private Queue<PreprocessedInput> outputQueue = new Queue<PreprocessedInput>();

        public InputPreprocessorPipeline(IInputPreprocessor[] inputPreprocessor)
        {
            this.inputPreprocessors = inputPreprocessor ?? Array.Empty<IInputPreprocessor>();
        }

        public IEnumerable<char> Process(char input)
        {
            if (this.inputPreprocessors.Length == 0)
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