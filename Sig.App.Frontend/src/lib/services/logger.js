import { Client } from "@/lib/helpers/client";

const MAX_MSG_QUEUE = 5;

const LOG_LEVEL_INFORMATION = "Information";
const LOG_LEVEL_WARNING = "Warning";
const LOG_LEVEL_ERROR = "Error";
const LOG_LEVEL_CRITICAL = "Critical";

let logMessage = [];
let sendMsgTimeout = null;

const loggerService = {
  logInformation: (msg) => log(msg, LOG_LEVEL_INFORMATION),
  logWarning: (msg) => log(msg, LOG_LEVEL_WARNING),
  logError: (msg) => log(msg, LOG_LEVEL_ERROR),
  logCritical: (msg) => log(msg, LOG_LEVEL_CRITICAL)
};

export function LoggerPlugin(app) {
  app.config.globalProperties.$logger = loggerService;
}

export default loggerService;

function log(message, level) {
  if (!message) message = "";
  else message = message.toString();

  logMessage.push({ message, level });
  resetOrSend();
}

async function sendMsg() {
  try {
    Client.post(`/log`, JSON.stringify(logMessage));
  } catch (error) {
    //silent error
    return;
  }
  logMessage = [];
}

function resetOrSend() {
  if (logMessage.length >= MAX_MSG_QUEUE) {
    sendMsg();
    clearTimeout(sendMsgTimeout);
  } else {
    resetTimeout();
  }
}

function resetTimeout() {
  clearTimeout(sendMsgTimeout);
  startTimeout();
}

function startTimeout() {
  sendMsgTimeout = setTimeout(sendMsg, 1000);
}
