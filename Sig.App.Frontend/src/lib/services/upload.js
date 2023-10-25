/* eslint-disable @intlify/vue-i18n/no-missing-keys */
import i18n from "@/lib/i18n";
import { Client } from "@/lib/helpers/client";

export async function uploadTempFile(file) {
  const { fileId } = await upload("tempFile", file);
  return fileId;
}

export async function uploadImage(file) {
  const { fileId } = await upload("image", file);
  return fileId;
}

async function upload(uploadType, file) {
  const data = new FormData();
  data.append("file", file);

  const response = await Client.post(`/upload/${uploadType}`, data);

  if (response.status === 200) {
    return await response.data;
  } else {
    throw new Error(i18n.global.t("file-upload-error"));
  }
}
