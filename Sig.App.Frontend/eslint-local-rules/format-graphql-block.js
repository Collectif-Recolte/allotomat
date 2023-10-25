/**
 * @author tyankatsu
 * @copyright 2018 tyankatsu. All rights reserved.
 * See LICENSE file in root directory for full license.
 */
"use strict";

const prettier = require("prettier");
const prettierParser = "graphql";

module.exports = {
  meta: {
    type: "layout",
    fixable: "code",
    docs: {
      description: "Format fix for `<graphql>`in .vue. Using Prettier API",
      url: "https://github.com/gridsome/eslint-plugin-gridsome/blob/master/docs/rules/format-query-block.md"
    }
  },
  create(context) {
    const sourceCode = context.getSourceCode();
    const filePath = context.getFilename();
    return {
      Program(node) {
        if (!node.templateBody) {
          return;
        }
        const topLevelNodes = node.templateBody.parent.children;
        for (const node of topLevelNodes) {
          if (node.type === "VElement" && (node.name === "graphql" || node.name === "fragments")) {
            const codeRange = [node.startTag.range[1], node.endTag ? node.endTag.range[0] : node.range[1]];
            const code = sourceCode.text.slice(...codeRange).trim();
            const prettierConfig = prettier.resolveConfig.sync(filePath, {
              editorconfig: true
            });

            let formattedCode;

            try {
              formattedCode = prettier
                .format(
                  code,
                  Object.assign({}, prettierConfig, {
                    parser: prettierParser
                  })
                )
                .trim();
            } catch {
              context.report({
                loc: node.loc,
                message: `Invalid ${node.name} code`
              });
              return;
            }

            if (formattedCode !== code) {
              context.report({
                loc: node.loc,
                message: `${node.name} code format is incorrect`,

                fix(fixer) {
                  return fixer.replaceTextRange(codeRange, `\n${formattedCode}\n`);
                }
              });
            }
          }
        }
      }
    };
  }
};
