using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace csx
{
    public class TestRewriter : CSharpSyntaxRewriter
    {
        private bool HasNotifyAttribute(PropertyDeclarationSyntax node)
        {
            return node.AttributeLists
                .Any(x => x.Attributes
                    .Any(y => y.Name.ToString() == "NotifyPropertyChanged"));
        }

        private string GetAutoField(string propertyName)
        {
            return string.Format("__csx__{0}", propertyName);
        }

        static AccessorDeclarationSyntax GetSet(PropertyDeclarationSyntax node)
        {
            return node.AccessorList.Accessors
                .SingleOrDefault(x => x.Keyword.Text == "set");
        }

        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            var children = node.Members;

            var notifyProps = children
                .OfType<PropertyDeclarationSyntax>()
                .Where(HasNotifyAttribute)
                .ToArray();

            if (!notifyProps.Any())
            {
                return base.VisitClassDeclaration(node);
            }

            var autoFields = notifyProps
                .Where(x => GetSet(x) != null && GetSet(x).Body == null)
                .Select(x => new
                {
                    Property = x,
                    Field = SyntaxFactory.FieldDeclaration(
                        SyntaxFactory.VariableDeclaration(
                            x.Type,
                            SyntaxFactory.SeparatedList(
                                new[]
                                {
                                    SyntaxFactory.VariableDeclarator(
                                        GetAutoField(x.Identifier.Text))
                                })))
                })
                .ToArray();
            
            foreach (var f in autoFields)
            {
                var members = node.Members.Insert(
                    node.Members.IndexOf(f.Property),
                    f.Field
                        .WithLeadingTrivia(SyntaxFactory.Whitespace("\r\n" + f.Property.GetLeadingTrivia()))
                        .WithTrailingTrivia(SyntaxFactory.Whitespace("\r\n")));

                node = node.WithMembers(members);
            }

            if (children
                .OfType<MethodDeclarationSyntax>()
                .Any(x => x.Identifier.ToString() == "InvokePropertyChanged"))
            {
                return base.VisitClassDeclaration(node);
            }

            node = node.AddMembers(GetInvokePropertyChanged());

            return base.VisitClassDeclaration(node);
        }

        public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            if (!HasNotifyAttribute(node))
            {
                return base.VisitPropertyDeclaration(node);
            }

            var setter = GetSet(node);

            if (setter == null)
            {
                return base.VisitPropertyDeclaration(node);
            }

            var call = SyntaxFactory.ExpressionStatement(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.IdentifierName("InvokePropertyChanged")));

            AccessorDeclarationSyntax newSetter;

            if (setter.Body == null)
            {
                var block = SyntaxFactory.Block(
                    SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            SyntaxFactory.IdentifierName(GetAutoField(node.Identifier.Text)),
                            SyntaxFactory.IdentifierName("value"))),
                    call);

                newSetter = setter.WithBody(block);
            }
            else
            {
                newSetter = setter.AddBodyStatements(call);
            }

            node = node.WithAccessorList(
                    node.AccessorList.WithAccessors(
                        node.AccessorList.Accessors
                            .Remove(setter)
                            .Add(newSetter)));

            return base.VisitPropertyDeclaration(node);
        }

        private MethodDeclarationSyntax GetInvokePropertyChanged()
        {
            var code = CSharpSyntaxTree.ParseText(
@"
        private void InvokePropertyChanged([CallerMemberName]string name = null)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }
");

            return code
                .GetRoot()
                .ChildNodes()
                .OfType<MethodDeclarationSyntax>()
                .Single();
        }
    }

    class Program
    {
        

        static void Main(string[] args)
        {
            //CSharpSyntaxRewriter
            var sw = new Stopwatch();
            sw.Start();
            var file = @"..\..\TestViewModel.cs";

            var ast = CSharpSyntaxTree.ParseText(File.ReadAllText(file));
            var astCode = ast.ToString();
            var root = new TestRewriter().Visit(ast.GetRoot());//.NormalizeWhitespace();
            var output = root.ToString();
            
            sw.Stop();
            Console.WriteLine("Done in {0}ms", sw.ElapsedMilliseconds);
            return;
            var descendants = ast
                .GetCompilationUnitRoot()
                .DescendantNodes()
                .OfType<PropertyDeclarationSyntax>()
                //.Where(x => x.
                .ToArray();
        }
    }
}
