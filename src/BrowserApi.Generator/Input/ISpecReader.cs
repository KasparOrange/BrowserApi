using BrowserApi.Generator.Ast;

namespace BrowserApi.Generator.Input;

public interface ISpecReader {
    IdlSpecFile ReadSpec(string filePath);
    IReadOnlyList<IdlSpecFile> ReadAllSpecs(string directoryPath);
}
