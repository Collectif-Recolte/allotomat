import { Client } from "@/lib/helpers/client";

async function decrypt(qrCode) {
  const response = await Client.post(
    "/qr-code/decrypt",
    JSON.stringify({
      qrCode
    })
  );

  return response.data;
}

export default {
  decrypt
};
