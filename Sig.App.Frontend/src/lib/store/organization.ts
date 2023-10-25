import { defineStore } from "pinia";

const getCurrentOrganization = () => {
  const currentOrganization = localStorage.getItem("currentOrganization");
  return currentOrganization ?? null;
};

export const useOrganizationStore = defineStore("organization", {
  state: () => ({
    currentOrganization: getCurrentOrganization()
  }),
  actions: {
    changeOrganization(newOrganization: string) {
      localStorage.setItem("currentOrganization", newOrganization);
      this.currentOrganization = newOrganization;
    }
  }
});
