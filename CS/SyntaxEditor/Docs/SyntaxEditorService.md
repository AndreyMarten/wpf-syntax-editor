# SyntaxEditorService

MVVM service that provides access to SyntaxEditor control functionality from ViewModels.

## Interface

```csharp
public interface ISyntaxEditorService {
    void MarkAsSaved();
    Task<IReadOnlyCollection<string>> GetLanguagesAsync(CancellationToken cancellationToken = default);
    void RegisterLanguage(LanguageDescriptor language);
}
```

## Methods

| Name | Description |
|------|-------------|
| MarkAsSaved() | Resets the IsModified flag of the associated SyntaxEditor to false. |
| GetLanguagesAsync(CancellationToken) | Asynchronously retrieves a collection of available programming language IDs from the associated SyntaxEditor. |
| RegisterLanguage(LanguageDescriptor) | Registers a custom programming language with Monarch tokenizer in the associated SyntaxEditor. |
