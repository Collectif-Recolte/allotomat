import { defineStore } from "pinia";

const getCurrentMarket = () => {
  const currentMarket = localStorage.getItem("currentMarket");
  return currentMarket ?? null;
};

export const useMarketStore = defineStore("market", {
  state: () => ({
    currentMarket: getCurrentMarket()
  }),
  actions: {
    changeCurrentMarket(newMarket: string) {
      localStorage.setItem("currentMarket", newMarket);
      this.currentMarket = newMarket;
    }
  }
});