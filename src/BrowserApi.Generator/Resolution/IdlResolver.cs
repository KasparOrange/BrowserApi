using BrowserApi.Generator.Ast;

namespace BrowserApi.Generator.Resolution;

public sealed class IdlResolver {
    public IdlResolvedModel Resolve(IReadOnlyList<IdlSpecFile> specFiles) {
        var model = new IdlResolvedModel();

        // Pass 1: Collect all primary definitions
        CollectDefinitions(specFiles, model);

        // Pass 2: Merge partial definitions
        MergePartials(specFiles, model);

        // Pass 3: Resolve includes (mixin members → target)
        ResolveIncludes(specFiles, model);

        // Pass 4: Store typedefs (already done in pass 1)

        // Pass 5: Build inheritance chains
        BuildInheritanceChains(model);

        return model;
    }

    private void CollectDefinitions(IReadOnlyList<IdlSpecFile> specFiles, IdlResolvedModel model) {
        foreach (var spec in specFiles) {
            foreach (var (name, def) in spec.Definitions) {
                switch (def) {
                    case IdlInterface iface when iface.Kind is "interface" or "namespace" or "callback interface":
                        model.Interfaces.TryAdd(name, iface);
                        break;
                    case IdlInterface mixin when mixin.Kind == "interface mixin":
                        // Store mixins temporarily in Interfaces for includes resolution
                        model.Interfaces.TryAdd(name, mixin);
                        break;
                    case IdlDictionary dict:
                        model.Dictionaries.TryAdd(name, dict);
                        break;
                    case IdlEnum e:
                        model.Enums.TryAdd(name, e);
                        break;
                    case IdlTypedef td:
                        model.Typedefs.TryAdd(name, td);
                        break;
                    case IdlCallback cb:
                        model.Callbacks.TryAdd(name, cb);
                        break;
                }
            }
        }
    }

    private void MergePartials(IReadOnlyList<IdlSpecFile> specFiles, IdlResolvedModel model) {
        foreach (var spec in specFiles) {
            foreach (var partial in spec.PartialDefinitions) {
                switch (partial) {
                    case IdlInterface partialIface:
                        if (model.Interfaces.TryGetValue(partial.Name, out var target)) {
                            target.Members.AddRange(partialIface.Members);
                            target.ExtAttrs.AddRange(partialIface.ExtAttrs);
                        } else {
                            model.Warnings.Add($"Partial interface '{partial.Name}' has no primary definition");
                        }
                        break;
                    case IdlDictionary partialDict:
                        if (model.Dictionaries.TryGetValue(partial.Name, out var targetDict)) {
                            targetDict.Members.AddRange(partialDict.Members);
                            targetDict.ExtAttrs.AddRange(partialDict.ExtAttrs);
                        } else {
                            model.Warnings.Add($"Partial dictionary '{partial.Name}' has no primary definition");
                        }
                        break;
                }
            }
        }
    }

    private void ResolveIncludes(IReadOnlyList<IdlSpecFile> specFiles, IdlResolvedModel model) {
        var mixinsToRemove = new HashSet<string>();

        foreach (var spec in specFiles) {
            foreach (var inc in spec.IncludesStatements) {
                if (!model.Interfaces.TryGetValue(inc.Target, out var target)) {
                    model.Warnings.Add($"Includes target '{inc.Target}' not found");
                    continue;
                }
                if (!model.Interfaces.TryGetValue(inc.Includes, out var mixin)) {
                    model.Warnings.Add($"Includes mixin '{inc.Includes}' not found");
                    continue;
                }

                // Copy mixin members to target
                target.Members.AddRange(mixin.Members);
                mixinsToRemove.Add(inc.Includes);
            }
        }

        // Remove mixins from the output
        foreach (var name in mixinsToRemove) {
            if (model.Interfaces.TryGetValue(name, out var iface) && iface.Kind == "interface mixin")
                model.Interfaces.Remove(name);
        }
    }

    private void BuildInheritanceChains(IdlResolvedModel model) {
        foreach (var (name, iface) in model.Interfaces) {
            if (iface.Inheritance == null)
                continue;

            var chain = new List<string>();
            var current = iface.Inheritance;
            var visited = new HashSet<string> { name };

            while (current != null && visited.Add(current)) {
                chain.Add(current);
                if (model.Interfaces.TryGetValue(current, out var parent))
                    current = parent.Inheritance;
                else
                    break;
            }

            if (chain.Count > 0)
                model.InheritanceChains[name] = chain;
        }

        foreach (var (name, dict) in model.Dictionaries) {
            if (dict.Inheritance == null)
                continue;

            var chain = new List<string>();
            var current = dict.Inheritance;
            var visited = new HashSet<string> { name };

            while (current != null && visited.Add(current)) {
                chain.Add(current);
                if (model.Dictionaries.TryGetValue(current, out var parent))
                    current = parent.Inheritance;
                else
                    break;
            }

            if (chain.Count > 0)
                model.InheritanceChains[name] = chain;
        }
    }
}
