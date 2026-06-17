import { filterOptions } from "@/../pinkflamant/lib/filter-options";

describe("filter-options.js", () => {
  const options = [
    { label: "École primaire", value: "1" },
    { label: "Garderie", value: "2", isDisabled: true },
    { label: "CPE", value: "3" }
  ];

  it("returns all options when query is empty", () => {
    expect(filterOptions(options, "")).toEqual(options);
  });

  it("filters by normalized query (accents and case)", () => {
    expect(filterOptions(options, "ecole")).toEqual([{ label: "École primaire", value: "1" }]);
  });

  it("excludes disabled options when filtering", () => {
    expect(filterOptions(options, "gard")).toEqual([]);
  });
});
