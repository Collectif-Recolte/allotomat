import { read, utils } from "xlsx";

async function readFile(file) {
  const wb = read(await file.arrayBuffer(), { dateNF: "mm/dd/yyyy" });
  return utils.sheet_to_json(wb.Sheets[wb.SheetNames[0]], { raw: false });
}

export default {
  readFile
};
