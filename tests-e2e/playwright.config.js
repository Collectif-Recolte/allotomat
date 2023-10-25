const { devices } = require("@playwright/test");

const config = {
  use: {
    baseURL: process.env.BASE_URL || "http://localhost:61759/"
  },
  projects: [
    {
      name: "chromium",
      use: { ...devices["Desktop Chrome"] }
    },
    {
      name: "firefox",
      use: { ...devices["Desktop Firefox"] }
    },
    {
      name: "webkit",
      use: { ...devices["Desktop Safari"] }
    },
    {
      name: "ios",
      use: { ...devices["iPhone 13"] }
    },
    {
      name: "android",
      use: { ...devices["Pixel 5"] }
    }
  ]
};

module.exports = config;
