import fs from "node:fs";

const jsonFile = fs.readFileSync(
  "./Bow.Compiler/Syntax/SyntaxNodes.json",
  "ascii"
);

const syntaxNodesFile = fs.createWriteStream(
  "./Bow.Compiler/Syntax/SyntaxNodes.g.cs",
  "utf-8"
);

syntaxNodesFile.write(`#nullable enable
namespace Bow.Compiler.Syntax;`);

const syntaxFactoryFile = fs.createWriteStream(
  "./Bow.Compiler/Syntax/SyntaxFactory.g.cs",
  "utf-8"
);

syntaxFactoryFile.write(`#nullable enable
namespace Bow.Compiler.Syntax;

public partial class SyntaxFactory
{`);

const { definitions } = JSON.parse(jsonFile);

function toCamelCase(name) {
  return name[0].toLowerCase() + name.substring(1);
}

function processNames([name, typeOrOpts]) {
  let typeName = typeOrOpts.type ? typeOrOpts.type : typeOrOpts;
  typeName =
    typeName === "Token" || typeName === "IdentifierToken"
      ? typeName
      : `${typeName}Syntax`;

  typeName = typeOrOpts.array ? `SyntaxList<${typeName}>` : typeName;
  typeName = typeOrOpts.optional ? `${typeName}?` : typeName;
  const modifier = typeOrOpts.override ? "override " : "";
  return [name, typeName, toCamelCase(name), modifier];
}

function first(children) {
  const props = [];
  for (const name in children) {
    if (children[name].optional) {
      props.push(name);
      continue;
    }

    props.push(name);
    break;
  }

  return props.length === 1 ? props[0] : props.join("?.Location ?? ");
}

function last(children) {
  const props = [];
  const entries = Object.entries(children);
  for (let i = entries.length - 1; i >= 0; i--) {
    if (entries[i][1].optional) {
      props.push(entries[i][0]);
      continue;
    }

    props.push(entries[i][0]);
    break;
  }

  return props.length === 1 ? props[0] : props.join("?.Location ?? ");
}

for (const name in definitions) {
  const { abstract, extends: ex, children } = definitions[name];
  const properties = Object.entries(children || {}).map(processNames);

  if (!abstract) {
    syntaxFactoryFile.write(`
    public ${name}Syntax ${name}(${properties
      .map(([, typeName, camelCaseName]) => `${typeName} ${camelCaseName}`)
      .join(", ")})
    {
        return new ${name}Syntax(SyntaxTree, ${properties
      .map(([, , camelCaseName]) => camelCaseName)
      .join(", ")});
    }
`);
  }

  syntaxNodesFile.write(`\n\npublic ${
    abstract ? "abstract" : "sealed"
  } class ${name}Syntax ${ex ? `: ${ex}Syntax` : ": SyntaxNode"}
{
    ${abstract ? "protected" : "public"} ${name}Syntax(${[
    [undefined, "SyntaxTree", "syntaxTree"],
    ...(abstract ? "" : properties),
  ]
    .map(([, typeName, camelCaseName]) => `${typeName} ${camelCaseName}`)
    .join(", ")})
      : base(syntaxTree)
    {
        ${
          abstract
            ? ""
            : properties
                .map(([name, , camelCaseName]) => `${name} = ${camelCaseName};`)
                .join("\n        ")
        }
    }${
      properties.length == 0
        ? ""
        : `

    ` +
          properties
            .map(
              ([name, typeName, , modifier]) =>
                `public ${
                  abstract ? "abstract " : modifier
                }${typeName} ${name} { get; }`
            )
            .join("\n    ")
    }${
    abstract
      ? ""
      : `

    public override Location Location => (${first(
      children
    )}.Location).Combine(${last(children)}.Location);`
  }
}`);
}

syntaxNodesFile.end();

syntaxFactoryFile.end("}");
