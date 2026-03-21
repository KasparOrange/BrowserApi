using System.Reflection;

namespace BrowserApi.Animations;

public sealed class KeyframeBuilder {
    private readonly List<Dictionary<string, object>> _frames = [];

    public KeyframeBuilder AddFrame(object properties) {
        _frames.Add(ToDictionary(properties));
        return this;
    }

    public KeyframeBuilder AddFrame(object properties, double offset) {
        var dict = ToDictionary(properties);
        dict["offset"] = offset;
        _frames.Add(dict);
        return this;
    }

    public KeyframeBuilder AddFrame(object properties, string easing) {
        var dict = ToDictionary(properties);
        dict["easing"] = easing;
        _frames.Add(dict);
        return this;
    }

    public KeyframeBuilder AddFrame(object properties, double offset, string easing) {
        var dict = ToDictionary(properties);
        dict["offset"] = offset;
        dict["easing"] = easing;
        _frames.Add(dict);
        return this;
    }

    public object Build() => _frames.ToArray();

    private static Dictionary<string, object> ToDictionary(object obj) {
        var dict = new Dictionary<string, object>();
        foreach (var prop in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
            var value = prop.GetValue(obj);
            if (value is not null)
                dict[prop.Name] = value;
        }
        return dict;
    }
}
