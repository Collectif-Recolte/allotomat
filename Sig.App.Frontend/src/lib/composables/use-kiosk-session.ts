import { computed, ref } from "vue";
import type { Ref } from "vue";
import { LOCAL_STORAGE_KIOSK_SESSION } from "@/lib/consts/local-storage";

export type KioskSessionEntry = {
  accessToken: string;
  expiresAt: string;
};

type KioskSessionStore = Record<string, KioskSessionEntry>;

const sessionVersion = ref(0);

function notifySessionChange() {
  sessionVersion.value++;
}

function readStore(): KioskSessionStore {
  try {
    const raw = localStorage.getItem(LOCAL_STORAGE_KIOSK_SESSION);
    if (!raw) return {};
    return JSON.parse(raw) as KioskSessionStore;
  } catch {
    return {};
  }
}

function writeStore(store: KioskSessionStore) {
  localStorage.setItem(LOCAL_STORAGE_KIOSK_SESSION, JSON.stringify(store));
}

export function getKioskSession(slug: string): KioskSessionEntry | null {
  const entry = readStore()[slug];
  if (!entry?.accessToken || !entry.expiresAt) return null;
  if (new Date(entry.expiresAt) <= new Date()) {
    clearKioskSession(slug);
    return null;
  }
  return entry;
}

export function setKioskSession(slug: string, accessToken: string, expiresAt: string) {
  const store = readStore();
  store[slug] = { accessToken, expiresAt };
  writeStore(store);
  notifySessionChange();
}

export function clearKioskSession(slug: string) {
  const store = readStore();
  delete store[slug];
  writeStore(store);
  notifySessionChange();
}

export function useKioskSession(slug: Ref<string | undefined>) {
  return computed(() => {
    sessionVersion.value;
    const key = slug.value;
    return key ? getKioskSession(key) : null;
  });
}
