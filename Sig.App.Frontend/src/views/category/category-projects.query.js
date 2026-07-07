import gql from "graphql-tag";

export const CATEGORY_PROJECTS_QUERY = gql`
  query Projects {
    projects {
      id
      beneficiaryTypes {
        id
        name
        keys
        beneficiaries {
          id
        }
      }
    }
  }
`;
