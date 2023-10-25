import * as langs from "./langs";
import * as urls from "./urls";

export function ConstsPlugin(app) {
  app.config.globalProperties.$consts = { langs, urls };
}
