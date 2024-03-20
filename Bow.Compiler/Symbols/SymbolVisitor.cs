namespace Bow.Compiler.Symbols;

public abstract class SymbolVisitor
{
    public abstract void VisitModuleSymbol(ModuleSymbol symbol);
    public abstract void VisitStructSymbol(StructSymbol symbol);
    public abstract void VisitEnumSymbol(EnumSymbol symbol);

    public virtual void VisitTypeSymbol(TypeSymbol symbol)
    {
        switch (symbol)
        {
            case StructSymbol structSymbol:
                VisitStructSymbol(structSymbol);
                break;
            case EnumSymbol enumSymbol:
                VisitEnumSymbol(enumSymbol);
                break;
            default:
                throw new UnreachableException();
        }
    }

    public abstract void VisitFunctionItemSymbol(FunctionItemSymbol symbol);
    public abstract void VisitFieldSymbol(FieldSymbol symbol);
    public abstract void VisitEnumCaseSymbol(EnumCaseSymbol symbol);
    public abstract void VisitMethodSymbol(MethodSymbol symbol);
    public abstract void VisitParameterSymbol(ParameterSymbol symbol);
}
