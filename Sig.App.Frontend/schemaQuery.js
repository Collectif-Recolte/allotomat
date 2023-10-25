// https://www.apollographql.com/docs/react/data/fragments/#generating-possibletypes-automatically
// Run with: node ./schemaQuery.js

const fs = require("fs");
const axios = require("axios").default;

axios({
  method: "post",
  url: "http://localhost:61534/graphql",
  headers: { "Content-Type": "application/json" },
  data: JSON.stringify({
    variables: {},
    query: `{
      __schema
      {
        types
        {
          kind 
          name 
          possibleTypes 
          { 
            name 
          } 
        } 
      }
    }`
  })
}).then((result) => {
  const possibleTypes = {};
  const data = result.data.data;
  data.__schema.types.forEach((supertype) => {
    if (supertype.possibleTypes) {
      possibleTypes[supertype.name] = supertype.possibleTypes.map((subtype) => subtype.name);
    }
  });

  fs.writeFile("./src/lib/graphql/possibleTypes.json", JSON.stringify(possibleTypes), (err) => {
    if (err) {
      // eslint-disable-next-line no-console
      console.error("Error writing possibleTypes.json", err);
    } else {
      // eslint-disable-next-line no-console
      console.log("Fragment types successfully extracted!");
    }
  });
});
