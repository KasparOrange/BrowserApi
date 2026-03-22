namespace BrowserApi.Dom;

public partial class Window {
    public BrowserApi.Console.Console Console => GetProperty<BrowserApi.Console.Console>("console");
}
