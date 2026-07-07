export function normalizeForSearch(str) {
  if (str == null) return "";
  return String(str)
    .normalize("NFD")
    .replace(/\p{Diacritic}/gu, "")
    .toLowerCase();
}
