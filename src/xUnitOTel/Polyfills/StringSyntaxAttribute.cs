#if !NET7_0_OR_GREATER
namespace System.Diagnostics.CodeAnalysis;

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Field | AttributeTargets.Property)]
internal sealed class StringSyntaxAttribute : Attribute
{
    public const string DateTimeFormat = nameof(DateTimeFormat);
    public StringSyntaxAttribute(string syntax) => Syntax = syntax;
    public string Syntax { get; }
}
#endif
