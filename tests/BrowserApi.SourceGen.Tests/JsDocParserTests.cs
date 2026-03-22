using BrowserApi.SourceGen;

namespace BrowserApi.SourceGen.Tests;

public class JsDocParserTests {
    [Fact]
    public void Parse_simple_exported_function() {
        var js = "export function greet(name) { return 'Hi ' + name; }";
        var functions = JsDocParser.Parse(js);

        Assert.Single(functions);
        Assert.Equal("greet", functions[0].JsName);
        Assert.Equal("GreetAsync", functions[0].CSharpName);
        Assert.Single(functions[0].Params);
        Assert.Equal("name", functions[0].Params[0].Name);
    }

    [Fact]
    public void Parse_function_with_jsdoc() {
        var js = @"
/**
 * Formats a number as currency.
 * @param {number} amount - The amount to format.
 * @param {string} currency - ISO currency code.
 * @returns {string} The formatted string.
 */
export function formatCurrency(amount, currency) {
    return amount.toString();
}";
        var functions = JsDocParser.Parse(js);

        Assert.Single(functions);
        var func = functions[0];
        Assert.Equal("formatCurrency", func.JsName);
        Assert.Equal("FormatCurrencyAsync", func.CSharpName);
        Assert.Equal("Formats a number as currency.", func.Summary);
        Assert.Equal("string", func.ReturnType);
        Assert.Equal("The formatted string.", func.ReturnsDoc);
        Assert.Equal(2, func.Params.Count);
        Assert.Equal("double", func.Params[0].CSharpType);
        Assert.Equal("The amount to format.", func.Params[0].Description);
        Assert.Equal("string", func.Params[1].CSharpType);
    }

    [Fact]
    public void Parse_void_return() {
        var js = @"
/**
 * Logs a message.
 * @param {string} msg - The message.
 * @returns {void}
 */
export function logMsg(msg) { console.log(msg); }";
        var functions = JsDocParser.Parse(js);

        Assert.Single(functions);
        Assert.Null(functions[0].ReturnType); // void → null
    }

    [Fact]
    public void Parse_multiple_functions() {
        var js = @"
export function foo() {}
export function bar(x) {}
export function baz(a, b, c) {}";
        var functions = JsDocParser.Parse(js);

        Assert.Equal(3, functions.Count);
        Assert.Equal("foo", functions[0].JsName);
        Assert.Equal("bar", functions[1].JsName);
        Assert.Equal("baz", functions[2].JsName);
        Assert.Equal(3, functions[2].Params.Count);
    }

    [Fact]
    public void Parse_skips_non_exported_functions() {
        var js = @"
function internal() {}
export function exported() {}";
        var functions = JsDocParser.Parse(js);

        Assert.Single(functions);
        Assert.Equal("exported", functions[0].JsName);
    }

    [Fact]
    public void Parse_no_duplicate_from_jsdoc_and_plain() {
        var js = @"
/**
 * Does something.
 * @param {number} x - A number.
 */
export function doIt(x) {}";
        var functions = JsDocParser.Parse(js);

        Assert.Single(functions);
        Assert.Single(functions[0].Params);
    }

    [Fact]
    public void Parse_function_without_params() {
        var js = "export function noArgs() {}";
        var functions = JsDocParser.Parse(js);

        Assert.Single(functions);
        Assert.Empty(functions[0].Params);
    }

    [Fact]
    public void Parse_function_without_jsdoc_has_no_summary() {
        var js = "export function plain(x) {}";
        var functions = JsDocParser.Parse(js);

        Assert.Null(functions[0].Summary);
        Assert.Null(functions[0].ReturnType);
    }

    // Type mapping tests

    [Theory]
    [InlineData("number", "double")]
    [InlineData("string", "string")]
    [InlineData("boolean", "bool")]
    [InlineData("bool", "bool")]
    [InlineData("void", "void")]
    [InlineData("any", "object")]
    [InlineData("object", "object")]
    [InlineData("Object", "object")]
    [InlineData("undefined", "void")]
    public void MapType_handles_primitives(string jsType, string expected) {
        Assert.Equal(expected, JsDocParser.MapType(jsType));
    }

    [Fact]
    public void MapType_promise_unwraps() {
        Assert.Equal("string", JsDocParser.MapType("Promise<string>"));
        Assert.Equal("double", JsDocParser.MapType("Promise<number>"));
    }

    [Fact]
    public void MapType_array() {
        Assert.Equal("double[]", JsDocParser.MapType("Array<number>"));
        Assert.Equal("string[]", JsDocParser.MapType("string[]"));
    }

    [Fact]
    public void MapType_unknown_returns_object() {
        Assert.Equal("object", JsDocParser.MapType("SomeCustomType"));
    }

    [Fact]
    public void ToPascalCase_converts_correctly() {
        Assert.Equal("FormatCurrency", JsDocParser.ToPascalCase("formatCurrency"));
        Assert.Equal("X", JsDocParser.ToPascalCase("x"));
        Assert.Equal("Already", JsDocParser.ToPascalCase("Already"));
    }

    [Fact]
    public void Parse_multiline_summary() {
        var js = @"
/**
 * This is a function that does
 * something really important.
 * @param {number} x - Input.
 */
export function important(x) {}";
        var functions = JsDocParser.Parse(js);

        Assert.Contains("something really important", functions[0].Summary!);
    }

    [Fact]
    public void Parse_returns_without_description() {
        var js = @"
/**
 * Gets a value.
 * @returns {number}
 */
export function getValue() { return 42; }";
        var functions = JsDocParser.Parse(js);

        Assert.Equal("double", functions[0].ReturnType);
        Assert.Null(functions[0].ReturnsDoc);
    }

    [Fact]
    public void Parse_param_without_description() {
        var js = @"
/**
 * Test.
 * @param {string} name
 */
export function test(name) {}";
        var functions = JsDocParser.Parse(js);

        Assert.Equal("string", functions[0].Params[0].CSharpType);
        Assert.Null(functions[0].Params[0].Description);
    }
}
