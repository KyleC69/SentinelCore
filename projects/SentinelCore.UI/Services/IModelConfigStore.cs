// Solution: SentinelCore
// Project:   SentinelCore.UI
// File:         IModelConfigStore.cs
// Author: Kyle L. Crowder
// Build Num:  091112



using SentinelCore.Contracts.Contracts;




namespace SentinelCore.UI.Services;





/// <summary>
///     Persistence contract for the user-editable model configuration document.
///     The document stores one <see cref="ModelProfile" /> per configuration slot
///     (TheCore, MagManager, Utility) and survives application restarts.
/// </summary>
public interface IModelConfigStore
{
    /// <summary>
    ///     Loads the persisted model configuration document, or <c>null</c> when
    ///     no document has been saved yet.
    /// </summary>
    /// <returns>The persisted document, or <c>null</c> when absent or unreadable.</returns>
    ModelConfigDocument? Load();








    /// <summary>
    ///     Persists the model configuration document, creating the target
    ///     directory when it does not exist.
    /// </summary>
    /// <param name="document">The document to persist.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="document" /> is <c>null</c>.</exception>
    void Save(ModelConfigDocument document);
}