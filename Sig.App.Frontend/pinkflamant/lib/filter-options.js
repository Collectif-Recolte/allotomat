import { normalizeForSearch } from "./normalize-for-search";

export function filterOptions(options, query) {
  if (!query) {
    return options;
  }
  const searchTerm = normalizeForSearch(query);
  return options.filter(
    (option) => !option.isDisabled && normalizeForSearch(option.label).includes(searchTerm)
  );
}
