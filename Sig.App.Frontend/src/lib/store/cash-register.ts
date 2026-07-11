import { defineStore } from "pinia";

const getCurrentCashRegister = () => {
  const currentCashRegister = localStorage.getItem("currentCashRegister");
  return currentCashRegister ?? null;
};

export const useCashRegisterStore = defineStore("cashRegister", {
  state: () => ({
    currentCashRegister: getCurrentCashRegister()
  }),
  actions: {
    changeCashRegister(newCashRegister: string | null) {
      if (newCashRegister === null) {
        localStorage.removeItem("currentCashRegister");
      } else {
        localStorage.setItem("currentCashRegister", newCashRegister);
      }
      this.currentCashRegister = newCashRegister;
    }
  }
});
