import axios from "axios";
import { VUE_APP_ROOT_API } from "@/env";

export const Client = axios.create({
  baseURL: `${VUE_APP_ROOT_API}`,
  headers: { "Content-Type": "application/json" }
});
