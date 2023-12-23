using Lifti.Querying;
using Lifti.Tokenization;
using Lifti.Tokenization.TextExtraction;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lifti
{
    /// <summary>
    /// </summary>
    public interface IFullTextIndex<TKey> : IIndexTokenizerProvider
    {
        /// <inheritdoc cref="Metadata"/>
        [Obsolete("Use the Metadata property instead")]
        IIndexMetadata<TKey> Items { get; }

        /// <summary>
        /// The <see cref="IIndexMetadata{TKey}"/> keeps track index metadata, including maps between internal document ids and keys, 
        /// statistics about token counts and score boost aggregates. 
        /// </summary>
        IIndexMetadata<TKey> Metadata { get; }

        /// <summary>
        /// Fields are tracked internally as a id of type <see cref="byte"/>. This lookup can
        /// be used to get the field associated to an id, and visa versa.
        /// </summary>
        IIndexedFieldLookup FieldLookup { get; }

        /// <summary>
        /// Gets the number of documents contained in the index. This will not reflect any new documents
        /// that are currently being inserted in a batch until the batch completes.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Gets a self-consistent snapshot of the index that can be used to create navigators and query the index
        /// whilst it is being mutated.
        /// </summary>
        IIndexSnapshot<TKey> Snapshot { get; }

        /// <summary>
        /// Gets the configured <see cref="IQueryParser"/> for the index. If you need to execute the same query against the index multiple
        /// times, you can use this to parse a query as an <see cref="IQuery"/>, and then execute that against the index's <see cref="Search(IQuery)"/> method.
        /// </summary>
        IQueryParser QueryParser { get; }

        /// <summary>
        /// Gets the default <see cref="ITextExtractor"/> implementation that the index will use when one is
        /// not explicitly configured for a field.
        /// </summary>
        ITextExtractor DefaultTextExtractor { get; }

        /// <summary>
        /// Gets the default <see cref="IThesaurus"/> implementation that will be used while indexing text when one
        /// is not explicitly configured for a field.
        /// </summary>
        IThesaurus DefaultThesaurus { get; }

        /// <summary>
        /// Uses the current snapshot of the index to create an implementation of <see cref="IIndexNavigator"/> that can be used to 
        /// navigate through the index on a character by character basis. Provided as convenience for use instead of calling
        /// <see cref="IIndexSnapshot.CreateNavigator()" /> on <see cref="IFullTextIndex{T}.Snapshot" /> directly
        /// </summary>
        IIndexNavigator CreateNavigator();

        /// <summary>
        /// Indexes some text against a given key.
        /// </summary>
        /// <param name="key">The key of the document being indexed.</param>
        /// <param name="text">The text to index against the document.</param>
        /// <param name="cancellationToken">The optional <see cref="CancellationToken"/> for the operation.</param>
        Task AddAsync(TKey key, string text, CancellationToken cancellationToken = default);

        /// <summary>
        /// Indexes some text against a given key.
        /// </summary>
        /// <param name="key">The key of the document being indexed.</param>
        /// <param name="text">The text to index against the document.</param>
        /// <param name="cancellationToken">The optional <see cref="CancellationToken"/> for the operation.</param>
        Task AddAsync(TKey key, IEnumerable<string> text, CancellationToken cancellationToken = default);

        /// <summary>
        /// Indexes a single document extracted from type <typeparamref name="TObject"/>. This type must have been
        /// configured when the index was built.
        /// </summary>
        /// <typeparam name="TObject">
        /// The type of the object being indexed.
        /// </typeparam>
        /// <param name="item">
        /// The item to index.
        /// </param>
        /// <param name="cancellationToken">The optional <see cref="CancellationToken"/> for the operation.</param>
        Task AddAsync<TObject>(TObject item, CancellationToken cancellationToken = default);

        /// <summary>
        /// Indexes a set of documents extracted from type <typeparamref name="TObject"/>. This type must have been
        /// configured when the index was built.
        /// </summary>
        /// <typeparam name="TObject">
        /// The type of the object being indexed.
        /// </typeparam>
        /// <param name="items">
        /// The items to index.
        /// </param>
        /// <param name="cancellationToken">The optional <see cref="CancellationToken"/> for the operation.</param>
        Task AddRangeAsync<TObject>(IEnumerable<TObject> items, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes the document with the given key from this index. If the key is not indexed then
        /// this operation is a no-op and <c>false</c> is returned.
        /// </summary>
        /// <param name="key">
        /// The key of the document to remove.
        /// </param>
        /// <returns>
        /// <c>true</c> if the document was in the index, <c>false</c> if it was not.
        /// </returns>
        /// <param name="cancellationToken">The optional <see cref="CancellationToken"/> for the operation.</param>
        Task<bool> RemoveAsync(TKey key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs a search against this index.
        /// </summary>
        /// <param name="searchText">
        /// The query to use when searching in the index.
        /// </param>
        /// <returns>
        /// The matching search results.
        /// </returns>
        ISearchResults<TKey> Search(string searchText);

        /// <summary>
        /// Performs a search against this index.
        /// </summary>
        /// <param name="query">
        /// The query to use when searching in the index.
        /// </param>
        /// <returns>
        /// The matching search results.
        /// </returns>
        ISearchResults<TKey> Search(IQuery query);

        /// <summary>
        /// Starts a batch change within the index so that any subsequent mutations that are made using AddAsync and RemoveAsync will
        /// not be committed until <see cref="CommitBatchChangeAsync(CancellationToken)"/> is called. It is significantly more efficient
        /// to batch changes to an index, if possible.
        /// </summary>
        void BeginBatchChange();

        /// <summary>
        /// Commits a batch change that was started using <see cref="BeginBatchChange"/>.
        /// </summary>
        Task CommitBatchChangeAsync(CancellationToken cancellationToken = default);
    }
}