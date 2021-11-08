using Lifti;
using Lifti.Serialization.Binary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace TestConsole
{
    public static class ShardedIndexExample
    {
        public class ShardedIndex
        {
            private static readonly BinarySerializer<int> serializer = new BinarySerializer<int>();
            private readonly Dictionary<string, FullTextIndex<int>> indexShards = new Dictionary<string, FullTextIndex<int>>();
            private readonly SemaphoreSlim syncObject = new SemaphoreSlim(1);

            public async Task<FullTextIndex<int>> GetIndexAsync(string partitionKey, CancellationToken cancellationToken = default)
            {
                if (!await syncObject.WaitAsync(TimeSpan.FromSeconds(1), cancellationToken))
                {
                    throw new Exception("Timeout waiting for lock");
                }

                try
                {
                    if (!indexShards.TryGetValue(partitionKey, out var index))
                    {
                        // Create the index for this shard
                        index = new FullTextIndexBuilder<int>()
                                // e.g. shard peristance using the partition key as the file name
                                .WithIndexModificationAction(async (idx) =>
                                {
                                    using (var fileStream = File.OpenWrite($"{partitionKey}.dat"))
                                    {
                                        await serializer.SerializeAsync(idx, fileStream);
                                    }
                                })
                                .Build();

                        // Deserialize initial state, if appropriate
                        var serializedFile = new FileInfo($"{partitionKey}.dat");
                        if (serializedFile.Exists)
                        {
                            using (var stream = serializedFile.OpenRead())
                            {
                                await serializer.DeserializeAsync(index, stream);
                            }
                        }

                        // Store the index for the next access
                        indexShards[partitionKey] = index;
                    }

                    return index;
                }
                finally
                {
                    syncObject.Release();
                }
            }
        }
    }
}
